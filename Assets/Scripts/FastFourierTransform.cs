using System;
using UnityEngine;
using System.Numerics;

public class FastFourierTransform : MonoBehaviour
{
    /// <summary>
    /// Вычисление поворачивающего модуля e^(-i*2*PI*k/N)
    /// </summary>
    /// <param name="k"></param>
    /// <param name="N"></param>
    /// <returns></returns>
    public static Complex w(int k, int N)
    {
        if (k % N == 0) return 1;
        double arg = -2 * Math.PI * k / N;
        return new Complex(Math.Cos(arg), Math.Sin(arg));
    }
    public static Complex iw(int k, int N)
    {
        if (k % N == 0) return 1;
        double arg = 2 * Math.PI * k / N;
        return new Complex(Math.Cos(arg), Math.Sin(arg));
    }
    /// <summary>
    /// Возвращает спектр сигнала
    /// </summary>
    /// <param name="x">Массив значений сигнала. Количество значений должно быть степенью 2</param>
    /// <returns>Массив со значениями спектра сигнала</returns>
    public static Complex[] FFT(Complex[] x)
    {
        Complex[] X;
        int N = x.Length;
        if (N == 2)
        {
            X = new Complex[2];
            X[0] = x[0] + x[1];
            X[1] = x[0] - x[1];
        }
        else
        {
            Complex[] x_even = new Complex[N / 2];
            Complex[] x_odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++)
            {
                x_even[i] = x[2 * i];
                x_odd[i] = x[2 * i + 1];
            }
            Complex[] X_even = FFT(x_even);
            Complex[] X_odd = FFT(x_odd);
            X = new Complex[N];
            for (int i = 0; i < N / 2; i++)
            {
                X[i] = X_even[i] + w(i, N) * X_odd[i];
                X[i + N / 2] = X_even[i] - w(i, N) * X_odd[i];
            }
        }
        return X;
    }

    public static Complex[] iFFT(Complex[] x)
    {
        Complex[] X;
        int N = x.Length;
        if (N == 2)
        {
            X = new Complex[2];
            X[0] = x[0] + x[1];
            X[1] = x[0] - x[1];
        }
        else
        {
            Complex[] x_even = new Complex[N / 2];
            Complex[] x_odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++)
            {
                x_even[i] = x[2 * i];
                x_odd[i] = x[2 * i + 1];
            }
            Complex[] X_even = iFFT(x_even);
            Complex[] X_odd = iFFT(x_odd);
            X = new Complex[N];
            for (int i = 0; i < N / 2; i++)
            {
                X[i] = X_even[i] + iw(i, N) * X_odd[i];
                X[i + N / 2] = X_even[i] - iw(i, N) * X_odd[i];
            }
        }
        return X;
    }


    public static Complex[] ConvertFloatToComplex(float[] floatArray)
    {
        Complex[] complexArray = new Complex[floatArray.Length];
        for (int i = 0; i < floatArray.Length; i++)
        {
            complexArray[i] = new Complex(floatArray[i], 0);
        }
        return complexArray;
    }


    public static float[] ConvertComplexToFloat(Complex[] complex)
    {
        float[] floatArray = new float[complex.Length];
        for (int i = 0; i < complex.Length; i++)
        {
            floatArray[i] = (float)complex[i].Magnitude;
        }
        return floatArray;
    }
    public static float[] ConvertComplexToFloat_Real(Complex[] complex)
    {
        float[] floatArray = new float[complex.Length];
        for (int i = 0; i < complex.Length; i++)
        {
            floatArray[i] = (float)complex[i].Real;
        }
        return floatArray;
    }
}
