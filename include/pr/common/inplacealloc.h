//-----------------------------------------------------------------------------
// File: InPlaceAlloc.h
//
// Desc: Custom STL allocator that uses a memory region passed by the caller.
//       This allocator never frees the memory directly; that's the
//       responsibility of the caller. For simplicity, this allocator will
//       also not reuse freed memory. Not only can callers pass heap memory
//       to this allocator, they can also pass stack memory, which can be
//       extremely efficient when the container is only used in the same scope
//       as the stack memory.
//
//       This allocator can be very useful in the case where a container 
//       is used in a localized section of code. The caller can allocate a chunk
//       of memory or declare some memory on the stack. The caller can then
//       create an InPlace allocator and pass that allocator to the container.
//       When the container goes out of scope, the caller can free the chunk
//       of memory.
//
// Hist: 10.11.02 - New for November 2002 XDK release
//       02.27.02 - Updated to work properly with containers like deques which
//                  have two copies of allocators. Added reference counting.
//
// Example Usage 1 (stack memory):
//
//     // All allocations for v will be on the stack
//     BYTE pStack[1024];
//     InPlaceAlloc< int > alloc( pStack, 1024 );
//     std::vector< int, InPlaceAlloc< int > > v( alloc );
//
// Example Usage 2 (typedefs):
//
//     // All allocations for v will be on the stack
//     typedef InPlaceAlloc< int > MyIntAlloc;
//     typedef std::vector< int, MyIntAlloc > MyIntVector;
//     BYTE pStack[1024];
//     MyIntAlloc alloc( pStack, 1024 );
//     MyIntVector v( alloc );
//
// Example Usage 3 (heap memory):
//
//     // All allocations for v will be from pHeap
//     BYTE* pHeap = (BYTE*)malloc( 1024 );
//     MyIntAlloc alloc( pHeap, 1024 );
//     MyIntVector v( alloc );
//     // Free pHeap after v goes out of scope
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#pragma once
#ifndef PR_IN_PLACE_ALLOC_H
#define PR_IN_PLACE_ALLOC_H

#include <memory>
#include <cassert>


//-----------------------------------------------------------------------------
// Name: InPlaceAlloc()
// Desc: Allocator that uses memory from the caller
//-----------------------------------------------------------------------------
template< typename T >
class InPlaceAlloc
{
private:

    // Forward declaration
    struct MemInfo;

public:

    //-------------------------------------------------------------------------
    // Boilerplate allocator typedefs
    //-------------------------------------------------------------------------
    typedef size_t    size_type;
    typedef ptrdiff_t difference_type;
    typedef T*        pointer;
    typedef const T*  const_pointer;
    typedef T&        reference;
    typedef const T&  const_reference;
    typedef T         value_type;

    //-------------------------------------------------------------------------
    // Constructors/Destructor
    //-------------------------------------------------------------------------
    InPlaceAlloc( unsigned char* pBuffer, size_t nBytes )
    :
        m_pMemInfo( new MemInfo( pBuffer, nBytes ) )
    {
        assert( pBuffer != NULL );
        assert( nBytes > 0 );
    }

    InPlaceAlloc( const InPlaceAlloc< T >& alloc )
    :
        m_pMemInfo( alloc.m_pMemInfo )
    {
        ++m_pMemInfo->nRefCount;
    }

    template< typename U >
    InPlaceAlloc( const InPlaceAlloc< U >& alloc )
    :
        // Must cast because we are constructing a T allocator from a U allocator
        m_pMemInfo( (InPlaceAlloc<T>::MemInfo*)( alloc.m_pMemInfo ) )
    {
        ++m_pMemInfo->nRefCount;
    }

    ~InPlaceAlloc()
    {
        if( --m_pMemInfo->nRefCount == 0 )
            delete m_pMemInfo;
    }

    //-------------------------------------------------------------------------
    // Boilerplate allocator functions
    //-------------------------------------------------------------------------
    template< typename U >
    struct rebind
    {
        typedef InPlaceAlloc< U > other;
    };

    pointer address( reference x ) const
    {   
        return &x;
    }

    const_pointer address( const_reference x ) const
    {
        return &x;
    }

    void construct( pointer p, const T& val )
    {
        new ((void *)p) T(val); // placement new
    }

    void destroy( pointer p )
    {
        (p)->~T(); // in-place destruction
    }

    size_t max_size() const // maximum array size
    {
        size_t nCount = m_pMemInfo->nMaxBytes / sizeof( T );
        return( 0 < nCount ? nCount : 1 );
    }

    //-------------------------------------------------------------------------
    // Name: allocate
    // Desc: Allocates memory from the buffer
    //-------------------------------------------------------------------------
    pointer allocate( size_type nCount )
    {
        return allocate( nCount, NULL );
    }

    pointer allocate( size_type nCount, const void* /* pHint */ )
    {
        // A buffer must be specified when constructing the allocator
        assert( m_pMemInfo->pBuffer != NULL );
    
        // Find the first block of unused memory
        pointer p = (pointer)( m_pMemInfo->pBuffer + m_pMemInfo->nBytesAllocated );
        
        // Update the status of the block
        m_pMemInfo->nBytesAllocated += ( nCount * sizeof( T ) );
        
        // For C++ Standard compliance, throw bad_alloc when we exceed the
        // limitations of the supplied memory block.
        // If your code is expecting NULL in failure cases, change this
        // code to return NULL.
        if( m_pMemInfo->nBytesAllocated + 1 > m_pMemInfo->nMaxBytes )
            throw std::bad_alloc();
            
        return p;
    }

    //-------------------------------------------------------------------------
    // Name: deallocate
    // Desc: Deallocate memory using CRT function free
    //-------------------------------------------------------------------------
    void deallocate( pointer, size_type /* nCount */ )
    {
        // For maximum speed, do nothing!
        
        // A slightly smarter/slower allocator could determine if p refers 
        // to the last block allocated by allocate(), and could adjust 
        // m_pMemInfo->nBytesAllocated appropriately.
        
        // An even smarter/slower allocator could maintain a list of 
        // allocated blocks, but then you'd lose the whole speed advantage 
        // of this allocator.
    }

    //-------------------------------------------------------------------------
    // Accessor functions
    //-------------------------------------------------------------------------
    const void* GetMemInfo() const
    { 
        // Return void for two reasons:
        // 1) Makes the pointer opaque to the world
        // 2) Makes comparing pointers easy, since there are no private 
        //    types involved
        return m_pMemInfo;
    }
    
private:

    struct MemInfo
    {
        unsigned char* pBuffer;     // pointer to the original memory block
        size_t nMaxBytes;           // size of the original memory block
        size_t nBytesAllocated;     // number of bytes of block allocated
        size_t nRefCount;           // ref count of this object
        
        MemInfo()
        :
            pBuffer( NULL ),
            nMaxBytes( 0 ),
            nBytesAllocated( 0 ),
            nRefCount( 1 )
        {
        }
        
        MemInfo( unsigned char* pBuf, size_t nMax )
        :
            pBuffer( pBuf ),
            nMaxBytes( nMax ),
            nBytesAllocated( 0 ),
            nRefCount( 1 )
        {
        }
        
    private:
    
        // Disabled
        MemInfo( const MemInfo& );
        MemInfo& operator=( const MemInfo& );
    
    };
    
private:

    // Reference counted object that keeps track of the original
    // chunk of memory. Must be reference counted to properly work
    // with containers that may contain multiple allocators. For instance,
    // deques commonly contain an allocator for the map and an allocator
    // for individual values.
    MemInfo* m_pMemInfo;

    // Grant all specializations access private data.
    // This is required for the template copy ctor.
    template <typename U> friend class InPlaceAlloc;

    // Disabled; must pass a chunk of valid memory
    InPlaceAlloc();

    // Unused
    InPlaceAlloc< T >& operator=( const InPlaceAlloc< T >& );

};


//-----------------------------------------------------------------------------
// InPlaceAlloc standard template operators
//-----------------------------------------------------------------------------
template< typename T, typename U >
inline bool operator==( const InPlaceAlloc< T >& lhs, const InPlaceAlloc< U >& rhs )
{
    return lhs.GetMemInfo() == rhs.GetMemInfo();
}

template< typename T, typename U >
inline bool operator!=( const InPlaceAlloc< T >& lhs, const InPlaceAlloc< U >& rhs )
{
    return lhs.GetMemInfo() != rhs.GetMemInfo();
}


//-----------------------------------------------------------------------------
// Specialize for void
//-----------------------------------------------------------------------------
template<>
class InPlaceAlloc< void >
{
public:

    //-------------------------------------------------------------------------
    // Boilerplate allocator typedefs; no references to void possible
    //-------------------------------------------------------------------------
    typedef void*       pointer;
    typedef const void* const_pointer;
    typedef void        value_type;

    //-------------------------------------------------------------------------
    // Constructors
    //-------------------------------------------------------------------------
    InPlaceAlloc() 
    {
    }

    template< typename U >
    InPlaceAlloc( const InPlaceAlloc< U >& )
    {
    }

    //-------------------------------------------------------------------------
    // Boilerplate rebind
    //-------------------------------------------------------------------------
    template< typename U >
    struct rebind
    {
        typedef InPlaceAlloc< U > other;
    };
};


#endif // IN_PLACE_ALLOC_H
