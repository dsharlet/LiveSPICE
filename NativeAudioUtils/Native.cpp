// Native.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Native.h"

template <typename T>
inline T clamp(T x, T min, T max)
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

extern "C" NATIVE_API void Fixed16x1ToDouble(const short * Fixed, double * Double, int Count)
{
	for (int i = 0; i < Count; ++i)
		Double[i] = (double)Fixed[i] * (1.0 / 32767.0);
}

extern "C" NATIVE_API void DoubleToFixed16x1(const double * Double, short * Fixed, int Count)
{
	for (int i = 0; i < Count; ++i)
		Fixed[i] = (short)(clamp(Double[i] * 32767.0, -32768.0, 32767.0));
}
