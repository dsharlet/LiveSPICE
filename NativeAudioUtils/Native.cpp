// Native.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Native.h"
#include <cstdlib>

template <typename T>
inline T clamp(T x, T min, T max)
{
	if (x < min) return min;
	if (x > max) return max;
	return x;
}

template <typename T>
inline T randf() { return (T)rand() / (T)RAND_MAX; }

template <typename T>
inline T dither(T x)
{
	return x + randf<T>();
}

template <int Bits, typename Fixed, typename Float>
void FixedToFloat(const Fixed * In, Float * Out, int Count)
{
	static const Float Max = (Float)((1UL << (Bits - 1)) - 1);

	for (int i = 0; i < Count; ++i)
		Out[i] = (Float)In[i] * (Float)(1.0 / Max);
}

template <int Bits, typename Float, typename Fixed>
void FloatToFixed(const Float * In, Fixed * Out, int Count)
{
	static const Float Max = (Float)((1UL << (Bits - 1)) - 1);

	for (int i = 0; i < Count; ++i)
		Out[i] = (Fixed)(clamp(dither(In[i] * Max), -Max, Max));
}

template <typename Float1, typename Float2>
void FloatToFloat(const Float1 * In, Float2 * Out, int Count)
{
	for (int i = 0; i < Count; ++i)
		Out[i] = (Float2)In[i];
}

extern "C" NATIVE_API void LEi16ToLEf32(const short * In, float * Out, int Count) { FixedToFloat<16>(In, Out, Count); }
extern "C" NATIVE_API void LEi16ToLEf64(const short * In, double * Out, int Count) { FixedToFloat<16>(In, Out, Count); }
extern "C" NATIVE_API void LEi32ToLEf32(const int * In, float * Out, int Count) { FixedToFloat<32>(In, Out, Count); }
extern "C" NATIVE_API void LEi32ToLEf64(const int * In, double * Out, int Count) { FixedToFloat<32>(In, Out, Count); }
extern "C" NATIVE_API void LEf32ToLEf64(const float * In, double * Out, int Count) { FloatToFloat(In, Out, Count); }

extern "C" NATIVE_API void LEf32ToLEi16(const float * In, short * Out, int Count) { FloatToFixed<16>(In, Out, Count); }
extern "C" NATIVE_API void LEf64ToLEi16(const double * In, short * Out, int Count) { FloatToFixed<16>(In, Out, Count); }
extern "C" NATIVE_API void LEf32ToLEi32(const float * In, short * Out, int Count) { FloatToFixed<32>(In, Out, Count); }
extern "C" NATIVE_API void LEf64ToLEi32(const double * In, short * Out, int Count) { FloatToFixed<32>(In, Out, Count); }
extern "C" NATIVE_API void LEf64ToLEf32(const double * In, float * Out, int Count) { FloatToFloat(In, Out, Count); }

                //case AsioWrapper.SampleType.Int16MSB:
                //case AsioWrapper.SampleType.Int24MSB:
                //case AsioWrapper.SampleType.Int32MSB:
                //case AsioWrapper.SampleType.Float32MSB:
                //case AsioWrapper.SampleType.Float64MSB:
                //case AsioWrapper.SampleType.Int32MSB16:
                //case AsioWrapper.SampleType.Int32MSB18:
                //case AsioWrapper.SampleType.Int32MSB20:
                //case AsioWrapper.SampleType.Int32MSB24:
                //case AsioWrapper.SampleType.Int16LSB:
                //case AsioWrapper.SampleType.Int24LSB:
                //case AsioWrapper.SampleType.Int32LSB:
                //case AsioWrapper.SampleType.Float32LSB:
                //case AsioWrapper.SampleType.Float64LSB:
                //case AsioWrapper.SampleType.Int32LSB16:
                //case AsioWrapper.SampleType.Int32LSB18:
                //case AsioWrapper.SampleType.Int32LSB20:
                //case AsioWrapper.SampleType.Int32LSB24:
                //case AsioWrapper.SampleType.DSDInt8LSB1:
                //case AsioWrapper.SampleType.DSDInt8MSB1:
                //case AsioWrapper.SampleType.DSDInt8NER8: