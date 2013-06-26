
#ifndef LIBSL_H
#define LIBSL_H

#include "../libopenjpeg/openjpeg.h"

struct MarshalledImage
{
	unsigned char* encoded;
	int length;
	int dummy; // padding for 64-bit alignment

	unsigned char* decoded;
	int width;
	int height;
	int layers;
	int resolutions;
	int components;
	int packet_count;
	opj_packet_info_t* packets;
};

#ifdef WIN32
#define DLLEXPORT extern "C" __declspec(dllexport)
#else
#define DLLEXPORT extern "C"
#endif

// uncompresed images are raw RGBA 8bit/channel
DLLEXPORT bool DotNetEncode(MarshalledImage* image, bool lossless);
DLLEXPORT bool DotNetDecode(MarshalledImage* image);
DLLEXPORT bool DotNetDecodeWithInfo(MarshalledImage* image);
DLLEXPORT bool DotNetAllocEncoded(MarshalledImage* image);
DLLEXPORT bool DotNetAllocDecoded(MarshalledImage* image);
DLLEXPORT void DotNetFree(MarshalledImage* image);

DLLEXPORT bool DotNetEncode64(MarshalledImage* image, bool lossless);
DLLEXPORT bool DotNetDecode64(MarshalledImage* image);
DLLEXPORT bool DotNetDecodeWithInfo64(MarshalledImage* image);
DLLEXPORT bool DotNetAllocEncoded64(MarshalledImage* image);
DLLEXPORT bool DotNetAllocDecoded64(MarshalledImage* image);
DLLEXPORT void DotNetFree64(MarshalledImage* image);

#endif
