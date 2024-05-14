using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TMPro;
using UnityEngine;
public class AnalysisFunctions : MonoBehaviour
{
  /// <summary>
  /// Shifts spectrum by changing original signal before Fourier Transform
  /// </summary>
  /// <param name="x">Original signal</param>
  /// <returns>Shifted signal</returns>
  public static Double[] Shift(Double[] x)
  {
    int N = x.Length;

    for (int i = 0; i < N; i++)
    {
      x[i] = (int)Math.Pow(-1, i) * x[i];
    }

    return x;
  }
  /// <summary>
  /// Complements (right) signal vector to the nearest power of 2 
  /// </summary>
  /// <param name="x">Signal samples</param>
  /// <returns>Complemented signal</returns>
  public static Double[] SignalPackRight(Double[] x)
  {
    int M = x.Length;

    int n = (int)Math.Ceiling(Math.Log(M, 2));

    int N = (int)Math.Pow(2, n);

    if (M == N)
    {
      return x;
    }
    else
    {
      Double[] y = new Double[N];

      int i = 0;

      for (i = 0; i < M; i++)
      {
        y[i] = x[i];
      }

      return y;
    }
  }

  /// <summary>
  /// Complements (left) signal vector to the nearest power of 2 
  /// </summary>
  /// <param name="x">Signal samples</param>
  /// <returns>Complemented signal</returns>
  public static Double[] SignalPackLeft(Double[] x)
  {
    int M = x.Length;

    int n = (int)Math.Ceiling(Math.Log(M, 2));

    int N = (int)Math.Pow(2, n);

    if (M == N)
    {
      return x;
    }
    else
    {
      int d = N - M;

      Double[] y = new Double[N];

      for (int i = d; i < N; i++)
      {
        y[i] = x[i - d];
      }

      return y;
    }
  }

  // channels 1 - left, 2 - right, any other - both
  // 3 might be buggy and display as 4 times reflection
  public static Complex[] PerformFFT(Double[] x, int channels = 1, bool shift = false)
  {
    Double[] signalpack3 = SignalPackBoth(x);
    if (channels == 1)
    {
      signalpack3 = SignalPackLeft(x);
    }
    else if (channels == 2)
    {
      signalpack3 = SignalPackRight(x);
    }
    Double[] signal = FFTDataSort(signalpack3);
    if (shift == true)
      signal = Shift(signal);
    return FFT(signal);
  }
  public static Double[] PerformInverseFFT(Complex[] x)
  {
    return Inverse_FFT(x);
  }
  public static Double[] SignalPackBoth(Double[] x)
  {
    int M = x.Length;

    int n = (int)Math.Ceiling(Math.Log(M, 2));

    int N = (int)Math.Pow(2, n);

    if (M == N)
    {
      return x;
    }
    else
    {
      int d = (int)(N - M) / 2;

      Double[] y = new Double[N];

      for (int i = d; i < M + d; i++)
      {
        y[i] = x[i - d];
      }

      return y;
    }
  }

  public static Double[] FFTDataSort(Double[] x)
  {
    int N = x.Length; // signal length

    if (Math.Log(N, 2) % 1 != 0)
    {
      throw new Exception("Number of samples in signal must be power of 2");
    }

    Double[] y = new Double[N]; // output (sorted) vector

    int BitsCount = (int)Math.Log(N, 2); // maximum number of bits in index binary representation

    for (int n = 0; n < N; n++)
    {
      string bin = Convert.ToString(n, 2).PadLeft(BitsCount, '0'); // index binary representation
      StringBuilder reflection = new StringBuilder(new string('0', bin.Length));

      for (int i = 0; i < bin.Length; i++)
      {
        reflection[bin.Length - i - 1] = bin[i]; // binary reflection
      }

      int number = Convert.ToInt32(reflection.ToString(), 2); // new index

      y[number] = x[n];

    }

    return y;

  }
  /// <summary>
  /// Calculates forward Fast Fourier Transform of given digital signal x
  /// </summary>
  /// <param name="x">Signal x samples values</param>
  /// <returns>Fourier Transform of signal x</returns>
  public static Complex[] FFT(Double[] x)
  {
    int N = x.Length; // Number of samples

    if (Math.Log(N, 2) % 1 != 0)
    {
      throw new Exception("Number of samples in signal must be power of 2");
    }


    Complex[] X = new Complex[N]; // Signal specturm

    // Rewrite real signal values to calculated spectrum
    for (int i = 0; i < N; i++)
    {
      X[i] = new Complex(x[i], 0);
    }

    int S = (int)Math.Log(N, 2); // Number of calculation stages

    for (int s = 1; s < S + 1; s++) // s - stage number
    {
      int BW = (int)Math.Pow(2, s - 1); // the width of butterfly
      int Blocks = (int)(Convert.ToDouble(N) / Math.Pow(2, s)); // Number of blocks in stage
      int BFlyBlocks = (int)Math.Pow(2, s - 1); // Number of butterflies in block
      int BlocksDist = (int)Math.Pow(2, s); // Distnace between blocks

      Complex W = Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI / Math.Pow(2, s)); // Fourier Transform kernel

      for (int b = 1; b < Blocks + 1; b++)
      {
        for (int m = 1; m < BFlyBlocks + 1; m++)
        {
          int itop = (b - 1) * BlocksDist + m; // top butterfly index
          int ibottom = itop + BW; // bottom butterfly index

          Complex Xtop = X[itop - 1]; // top element -> X(k)
          Complex Xbottom = X[ibottom - 1] * Complex.Pow(W, m - 1); // bottom element -> X(k + N/2)

          // Butterfly final calculation
          X[itop - 1] = Xtop + Xbottom;
          X[ibottom - 1] = Xtop - Xbottom;
        }
      }
    }

    // Spectrum scaling
    for (int i = 0; i < N; i++)
    {
      X[i] = X[i] / Convert.ToDouble(N);
    }

    return X;
  }

  /// <summary>
  /// Calculates inverse Fast Fourier Transform of given spectrum
  /// </summary>
  /// <param name="X">Spectrum values</param>
  /// <returns>Signal samples</returns>
  public static Double[] Inverse_FFT(Complex[] X)
  {
    int N = X.Length; // Number of samples
    Double[] x = new Double[N];
    int E = (int)Math.Log(N, 2);

    for (int e = 1; e < E + 1; e++)
    {
      int SM = (int)Math.Pow(2, e - 1);
      int LB = (int)(Convert.ToDouble(N) / Math.Pow(2, e));
      int LMB = (int)Math.Pow(2, e - 1);
      int OMB = (int)Math.Pow(2, e);

      Complex W = Complex.Exp(Complex.ImaginaryOne * 2 * Math.PI / Math.Pow(2, e)); // changed sign - our minor change

      for (int b = 1; b < LB + 1; b++)
      {
        for (int m = 1; m < LMB + 1; m++)
        {
          int g = (b - 1) * OMB + m;
          int d = g + SM;

          Complex xgora = X[g - 1];
          Complex xdol = X[d - 1] * Complex.Pow(W, m - 1);

          X[g - 1] = xgora + xdol;
          X[d - 1] = xgora - xdol;
        }
      }
    }

    for (int i = 0; i < N; i++)
    {
      x[i] = X[i].Real;
    }

    return x;
  }
}
