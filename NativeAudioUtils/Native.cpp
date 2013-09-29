// Native.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Native.h"

inline float clamp(float x, float min, float max)
{
	if (x < min) return min;
	if (x > max) return max;
	return x;
}

extern "C" NATIVE_API void Fixed16x1ToFloat(const short * Fixed, float * Float, int Count)
{
	for (int i = 0; i < Count; ++i)
		Float[i] = (float)Fixed[i] * (1.0f / 32767.0f);
}

extern "C" NATIVE_API void FloatToFixed16x1(const float * Float, short * Fixed, int Count)
{
	for (int i = 0; i < Count; ++i)
		Fixed[i] = (short)(clamp(Float[i] * 32767.0f, -32768.0f, 32767.0f));
}
