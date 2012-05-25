#include <stdio.h>
#include <conio.h>
#include "Network\UDPTransmitter\UDPTransmitter.h"
#include "Common\PRAssert.h"

byte CalculateCheckSum( const byte *data, byte data_length );

void main(void)
{
	printf( " -= UDP Talk =- \n\n" );
	
	UDPTransmitter transmitter;

	UDPTransmitterSettings settings;
	transmitter.Initialise(settings);

	char quit = 0;
	int send_count = 0;
	char str[256];
	while( quit != 'q' )
	{
		while( !kbhit() )	Sleep(50);
		quit = (char)getch();
		if( quit == 's' )
		{
			sprintf( str, "Test String %d\n", send_count++ );
			if( transmitter.Send( str, strlen(str) ) )
				printf( "Sender: sent %s",str );
		}
	}

	transmitter.KillAndBlockTillDead();
}

//*****
// Calculate a basic checksum for the data
byte CalculateCheckSum( const byte *data, byte data_length )
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
