using Emgu.CV;
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
		public static void DFTForward(string src, string dst)
		{
			var imgSrc = CvInvoke.Imread(src,LoadImageType.Grayscale | LoadImageType.AnyDepth);

			//get optimal dimensions (power of 2 i think..)
			int xdftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Rows);
			int ydftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Cols);

			//pad input image to optimal dimensions
			CvInvoke.CopyMakeBorder(imgSrc,imgSrc,
				0,xdftsz - imgSrc.Rows,0,ydftsz - imgSrc.Cols,
				BorderType.Constant,new MCvScalar(0)
			);
			imgSrc.PrintInfo("1");

			//use 32F format for calcs
			imgSrc.ConvertTo(imgSrc,DepthType.Cv32F);
			imgSrc.PrintInfo("2");
			//create a 2 channel mat using the input as the fist channel
			var planes = new VectorOfMat();
			planes.Push(imgSrc);
			planes.Push(new Mat(imgSrc.Size,DepthType.Cv32F,1));
			Mat complex = new Mat();
			CvInvoke.Merge(planes,complex);
			complex.PrintInfo("3");

			//do the fourrier transform
			CvInvoke.Dft(complex,complex,DxtType.Forward,0);
			complex.PrintInfo("4");

			//split channels into real / imaginary
			var compos = new VectorOfMat(2);
			CvInvoke.Split(complex,compos);

			//convert real / imaginary to magnitude / phase - which is easier to deal with when looking for artifacts
			Mat mag = new Mat();
			Mat phs = new Mat();
			CvInvoke.CartToPolar(compos[0],compos[1],mag,phs);
			mag.PrintInfo("5m");

			//convert to log scale since magnitude tends to have a huge range
			Helpers.AddS(mag,1.0,mag);
			CvInvoke.Log(mag,mag);
			mag.PrintInfo("6m");
			phs.PrintInfo("6p");

			//regular DFT puts low frequencies in the corners - this flips them to the center
			RearrangeQuadrants(mag);
			RearrangeQuadrants(phs);

			double magMax, magMin;
			CvInvoke.MinMaxIdx(mag,out magMin,out magMax,null,null);
			//Console.WriteLine("-mi "+magMin+","+magMax+"]");

			double phsMax, phsMin;
			CvInvoke.MinMaxIdx(phs,out phsMin,out phsMax,null,null);

			//convert to a 'normal' format and scale the data
			Mat magOut = new Mat();
			Mat phsOut = new Mat();

			CvInvoke.Normalize(mag,magOut,0,65535,NormType.MinMax,DepthType.Cv16U);
			CvInvoke.Normalize(phs,phsOut,0,65535,NormType.MinMax,DepthType.Cv16U);

			string name = Path.GetFileNameWithoutExtension(dst);

			magOut.PrintInfo("7m");
			phsOut.PrintInfo("7p");

			Console.WriteLine("-mi " + magMin + " -mx " + magMax + " -pi " + phsMin + " -px " + phsMax);

			magOut.Save(name+"-mag.png");
			phsOut.Save(name+"-phs.png");
		}

		public static void DFTInverse(IEnumerable<string> srcList, string dst, double mi, double mx, double pi, double px)
		{
			string magName = null, phsName = null;
			int index = 0;
			foreach(string src in srcList) {
				if (index == 0) { magName = src; }
				if (index == 1) { phsName = src; }
				index++;
			}

			var magSrc = CvInvoke.Imread(magName,LoadImageType.Grayscale | LoadImageType.AnyDepth);
			var phsSrc = CvInvoke.Imread(phsName,LoadImageType.Grayscale | LoadImageType.AnyDepth);

			magSrc.PrintInfo("1m");
			phsSrc.PrintInfo("1p");

			Mat mag = new Mat();
			Mat phs = new Mat();

			CvInvoke.Normalize(magSrc,mag,mi,mx,NormType.MinMax,DepthType.Cv32F);
			CvInvoke.Normalize(phsSrc,phs,pi,px,NormType.MinMax,DepthType.Cv32F);

			mag.PrintInfo("2m");
			phs.PrintInfo("2p");

			//flip these back to original positions
			RearrangeQuadrants(mag);
			RearrangeQuadrants(phs);

			//de-log the magnitude data
			CvInvoke.Exp(mag,mag);
			Helpers.AddS(mag,-1.0,mag);

			mag.PrintInfo("3m");

			//back to real / imaginary from magnitude / phase
			Mat real = new Mat();
			Mat imag = new Mat();
			CvInvoke.PolarToCart(mag,phs,real,imag);

			real.PrintInfo("4r");
			imag.PrintInfo("4i");

			//merge real / imaginary into one complex Mat
			var planes = new VectorOfMat();
			planes.Push(real);
			planes.Push(imag);
			Mat complex = new Mat();
			CvInvoke.Merge(planes,complex);
			complex.PrintInfo("5");

			//do the inverse fourrier transform
			CvInvoke.Dft(complex,complex,DxtType.Inverse,0);
			complex.PrintInfo("6");

			//split into spatial / (empty)
			var compos = new VectorOfMat(2);
			CvInvoke.Split(complex,compos);

			//the real part should contain the orignal data - we can throw away the imaginary part
			Mat img = compos[0]; 
			Mat imgOut = new Mat();
			CvInvoke.Normalize(img,imgOut,0,65535,NormType.MinMax,DepthType.Cv16U);

			imgOut.Save(dst);
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
	}
}
