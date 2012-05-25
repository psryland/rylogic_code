#include <stdio.h>
#include <conio.h>
#include "Network\UDPReceiver\UDPReceiver.h"
#include "Common\PRAssert.h"

#pragma comment(lib, "UDPReceiverD.lib")

byte CalculateCheckSum( const byte *data, byte data_length );

void main(void)
{
	printf( " -= UDP Listen =- \n\n" );
	
	UDPReceiverSettings settings;
	UDPReceiver receiver;
	receiver.Initialise(settings);

	char quit = 0;
	char str[256];
	memset( str, 0, 256 );
	while( quit != 'q' )
	{
		while( !kbhit() )
		{
			if( receiver.Receive( str, 256 ) > 0 )
			{
				printf( "Recv: received %s", str );
				memset( str, 0, 256 );
			}
		}
		quit = (char)getch();
	}

	receiver.KillAndBlockTillDead();
}

//*****
// Calculate a basic checksum for the data
byte CalculateCheckSum(const byte *data, byte data_length)
{
	int checksum = 0xAA; // 10101010
	int xor		 = 0xB3; // 10110011
	
	for( int i = 0; i < data_length; ++i )
	{
		checksum += data[i];
		xor		 ^= data[i];
	}
	
	return (byte)((checksum ^ xor) & 0xFF);
}
