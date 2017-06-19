﻿using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XPhoto;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace RasDenoise
{
	static class Methods
	{
		public static void NlMeans(string src, string dst, double h, int templateWindowSize, int searchWindowSize)
		{
			Console.WriteLine("Reading image "+src);
			var imgData = CvInvoke.Imread(src,LoadImageType.AnyColor);
			var outData = new Mat(imgData.Size,imgData.Depth,imgData.NumberOfChannels);

			Console.WriteLine("Denoising using "+nameof(NlMeans));
			CvInvoke.FastNlMeansDenoising(imgData,outData,(float)h,templateWindowSize,searchWindowSize);

			Console.WriteLine("Saving "+dst);
			outData.Bitmap.Save(dst);
		}

		public static void NlMeansColored(string src, string dst, double h, double hColor, int templateWindowSize, int searchWindowSize)
		{
			Console.WriteLine("Reading image "+src);
			var imgData = CvInvoke.Imread(src,LoadImageType.AnyColor);
			var outData = new Mat(imgData.Size,imgData.Depth,imgData.NumberOfChannels);

			Console.WriteLine("Denoising using "+nameof(NlMeansColored));
			CvInvoke.FastNlMeansDenoisingColored(imgData,outData,(float)h,(float)hColor,templateWindowSize,searchWindowSize);

			Console.WriteLine("Saving "+dst);
			outData.Bitmap.Save(dst);
		}

		public static void Dct(string src, string dst, double sigma, int psize)
		{
			Console.WriteLine("Reading image "+src);
			var imgData = CvInvoke.Imread(src,LoadImageType.AnyColor);
			var outData = new Mat();
		
			Console.WriteLine("Denoising using "+nameof(Dct));
			XPhotoInvoke.DctDenoising(imgData,outData,sigma,psize);
		
			Console.WriteLine("Saving "+dst);
			outData.Bitmap.Save(dst);
		}

		public static void TVL1(IEnumerable<string> srcList, string dst, double lambda, int niters)
		{
			var observations = new List<Mat>();
			foreach (string s in srcList)
			{
				Console.WriteLine("Reading image " + s);
				var imgData = CvInvoke.Imread(s, LoadImageType.AnyColor);
				observations.Add(imgData);
			}
			var outData = new Mat();

			Console.WriteLine("Denoising using " + nameof(TVL1));
			CvInvoke.DenoiseTVL1(observations.ToArray(), outData, lambda, niters);

			Console.WriteLine("Saving " + dst);
			outData.Bitmap.Save(dst);
		}


		//Followed this source code mostly
		//https://github.com/opencv-java/fourier-transform/blob/master/src/it/polito/teaching/cv/FourierController.java

		public static void FFTForward(string src, string dst)
		{
			var imgSrc = CvInvoke.Imread(src,LoadImageType.Grayscale);

			//get optimal dimensions (power of 2 i think..)
			int xdftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Rows);
			int ydftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Cols);

			//pad input image to optimal dimensions
			var imgPad = new Mat();
			CvInvoke.CopyMakeBorder(imgSrc,imgPad,
				0,xdftsz - imgSrc.Rows,0,ydftsz - imgSrc.Cols,
				BorderType.Constant,new MCvScalar(0)
			);
			imgPad.PrintInfo();

			//use 32F format for calcs
			imgPad.ConvertTo(imgPad,DepthType.Cv32F);
			imgPad.PrintInfo();
			//create a 2 channel mat using the input as the fist channel
			var planes = new VectorOfMat();
			planes.Push(imgPad);
			planes.Push(new Mat(imgPad.Size,DepthType.Cv32F,1));
			Mat complex = new Mat();
			CvInvoke.Merge(planes,complex);
			complex.PrintInfo();

			//do the fourrier transform
			CvInvoke.Dft(complex,complex,DxtType.Forward,0);
			complex.PrintInfo();

			//split channels into real / imaginary
			var compos = new VectorOfMat(2);
			CvInvoke.Split(complex,compos);

			//convert real / imaginary to magnitude / phase - which is easier to deal with when looking for artifacts
			Mat mag = new Mat();
			Mat phs = new Mat();
			CvInvoke.CartToPolar(compos[0],compos[1],mag,phs);

			//convert to log scale since magnitude tends to have a huge range
			Helpers.AddS(mag,1.0,mag);
			CvInvoke.Log(mag,mag);
			mag.PrintInfo();

			//regular DFT puts low frequencies in the corners - this flips them to the center
			RearrangeQuadrants(mag);
			RearrangeQuadrants(phs);

			//convert to a 'normal' format and scale the data
			mag.ConvertTo(mag,DepthType.Cv16U);
			CvInvoke.Normalize(mag,mag,0,65535,NormType.MinMax,DepthType.Cv16U);
			phs.ConvertTo(phs,DepthType.Cv16U);
			CvInvoke.Normalize(phs,phs,0,65535,NormType.MinMax,DepthType.Cv16U);

			string name = Path.GetFileNameWithoutExtension(dst);

			mag.Save(name+"-mag.png");
			phs.Save(name+"-phs.png");
		}

		static void RearrangeQuadrants(Mat image)
		{
			int cx = image.Width / 2;
			int cy = image.Height / 2;

			Mat q0 = new Mat(image,new Rectangle(0,0,cx,cy));
			Mat q1 = new Mat(image,new Rectangle(cx,0,cx,cy));
			Mat q2 = new Mat(image,new Rectangle(0,cy,cx,cy));
			Mat q3 = new Mat(image,new Rectangle(cx,cy,cx,cy));

			Mat tmp = new Mat();
			q0.CopyTo(tmp);
			q3.CopyTo(q0);
			tmp.CopyTo(q3);

			q1.CopyTo(tmp);
			q2.CopyTo(q1);
			tmp.CopyTo(q2);
		}

		public static void Test()
		{
			var x = new Emgu.CV.Superres.FrameSource(null,false);
			var y = new Emgu.CV.Superres.SuperResolution(Emgu.CV.Superres.SuperResolution.OpticalFlowType.Btvl,x);
		}
	}
}
