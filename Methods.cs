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
		public static void NlMeans(NlMeansArgs args)
		{
			Console.WriteLine("Reading image "+args.src);
			var imgData = CvInvoke.Imread(args.src,ImreadModes.AnyColor);
			var outData = new Mat(imgData.Size,imgData.Depth,imgData.NumberOfChannels);

			Console.WriteLine("Denoising using "+nameof(NlMeans));
			CvInvoke.FastNlMeansDenoising(imgData,outData,(float)args.h,args.templateWindowSize,args.searchWindowSize);

			Console.WriteLine("Saving "+args.dst);
			outData.Bitmap.Save(args.dst);
		}

		public static void NlMeansColored(NlMeansColoredArgs args)
		{
			Console.WriteLine("Reading image "+args.src);
			var imgData = CvInvoke.Imread(args.src,ImreadModes.AnyColor);
			var outData = new Mat(imgData.Size,imgData.Depth,imgData.NumberOfChannels);

			Console.WriteLine("Denoising using "+nameof(NlMeansColored));
			CvInvoke.FastNlMeansDenoisingColored(imgData,outData,(float)args.h,(float)args.hColor,args.templateWindowSize,args.searchWindowSize);

			Console.WriteLine("Saving "+args.dst);
			outData.Bitmap.Save(args.dst);
		}

		public static void Dct(DctArgs args)
		{
			Console.WriteLine("Reading image "+args.src);
			var imgData = CvInvoke.Imread(args.src,ImreadModes.AnyColor);
			var outData = new Mat();
		
			Console.WriteLine("Denoising using "+nameof(Dct));
			XPhotoInvoke.DctDenoising(imgData,outData,args.sigma.Value,args.psize);
		
			Console.WriteLine("Saving "+args.dst);
			outData.Bitmap.Save(args.dst);
		}

		public static void TVL1(TVL1Args args)
		{
			var observations = new List<Mat>();
			foreach (string s in args.srcList)
			{
				Console.WriteLine("Reading image " + s);
				var imgData = CvInvoke.Imread(s, ImreadModes.AnyColor);
				observations.Add(imgData);
			}
			var outData = new Mat();

			Console.WriteLine("Denoising using " + nameof(TVL1));
			CvInvoke.DenoiseTVL1(observations.ToArray(), outData, args.lambda.Value, args.niters.Value);

			Console.WriteLine("Saving " + args.dst);
			outData.Bitmap.Save(args.dst);
		}

		//Followed this source code mostly
		//https://github.com/opencv-java/fourier-transform/blob/master/src/it/polito/teaching/cv/FourierController.java
		public static void DFTForward(Helpers.AllFiles all)
		{
			var imgSrc = CvInvoke.Imread(all.Ori,ImreadModes.AnyColor | ImreadModes.AnyDepth);

			Mat magMat; Mat phsMat; Helpers.NormData[] normList;
			ForwardTransform(imgSrc,out magMat,out phsMat,out normList);

			magMat.Save(Helpers.GetSafeFileName(all.Mag));
			phsMat.Save(Helpers.GetSafeFileName(all.Phs));
			Helpers.SaveDtaFile(all.Dta,normList);
		}

		static void ForwardTransform(Mat src, out Mat magMat, out Mat phsMat, out Helpers.NormData[] normList)
		{
			Mat[] matList = src.Split();
			int chanCount = matList.Length;

			Mat[] magList = new Mat[chanCount];
			Mat[] phsList = new Mat[chanCount];
			normList = new Helpers.NormData[chanCount];
			for(int m=0; m<chanCount; m++)
			{
				Mat chanSrc = matList[m];
				Mat mag; Mat phs; Helpers.NormData norm;
				ForwardNormChannel(chanSrc, out mag, out phs, out norm);
				magList[m] = mag;
				phsList[m] = phs;
				normList[m] = norm;
			}

			var magVect = new VectorOfMat(magList);
			magMat = new Mat();
			CvInvoke.Merge(magVect,magMat);
			magMat.PrintInfo("MulChannel Mag");

			var phsVect = new VectorOfMat(phsList);
			phsMat = new Mat();
			CvInvoke.Merge(phsVect,phsMat);
			phsMat.PrintInfo("MulChannel Phs");
		}

		static void ForwardNormChannel(Mat imgSrc, out Mat magOut, out Mat phsOut, out Helpers.NormData norm)
		{
			Mat mag, phs;
			ForwardChannel(imgSrc,out mag, out phs);

			norm = new Helpers.NormData();
			CvInvoke.MinMaxIdx(mag,out norm.MagMin,out norm.MagMax,null,null);
			CvInvoke.MinMaxIdx(phs,out norm.PhsMin,out norm.PhsMax,null,null);

			//convert to a 'normal' format and scale the data
			magOut = new Mat();
			phsOut = new Mat();

			CvInvoke.Normalize(mag,magOut,0,65535,NormType.MinMax,DepthType.Cv16U);
			CvInvoke.Normalize(phs,phsOut,0,65535,NormType.MinMax,DepthType.Cv16U);

			magOut.PrintInfo("7m");
			phsOut.PrintInfo("7p");
		}

		static void ForwardChannel(Mat imgSrc, out Mat mag, out Mat phs)
		{
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
			mag = new Mat();
			phs = new Mat();
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
		}

		public static void DFTInverse(Helpers.AllFiles all)
		{
			var magSrc = CvInvoke.Imread(all.Mag,ImreadModes.AnyColor | ImreadModes.AnyDepth);
			var phsSrc = CvInvoke.Imread(all.Phs,ImreadModes.AnyColor | ImreadModes.AnyDepth);

			if (magSrc.Size != phsSrc.Size) {
				Console.WriteLine("Mag "+magSrc.Size+" and Phs "+phsSrc.Size+" need to be same size");
				return;
			}

			Helpers.NormData[] normList;
			bool w = Helpers.TryReadDtaFile(all.Dta,out normList);
			//TODO check w

			Mat orig;
			InverseTransform(magSrc,phsSrc,normList,out orig);

			orig.Save(Helpers.GetSafeFileName(all.Ori));
		}

		public static void InverseTransform(Mat magSrc, Mat phsSrc, Helpers.NormData[] normList, out Mat orig)
		{
			int chanCount = magSrc.NumberOfChannels;
			Mat[] magList = magSrc.Split();
			Mat[] phsList = phsSrc.Split();
			Mat[] oriList = new Mat[chanCount];

			for(int c=0; c<chanCount; c++)
			{
				Mat ori;
				InverseNormChannel(magList[c], phsList[c], normList[c],out ori);
				oriList[c] = ori;
			}

			var chanVect = new VectorOfMat(oriList);
			orig = new Mat();
			CvInvoke.Merge(chanVect,orig);
			orig.PrintInfo("MultiOriginal");
		}

		static void InverseNormChannel(Mat magSrc, Mat phsSrc, Helpers.NormData norm, out Mat imgOut)
		{
			magSrc.PrintInfo("1m");
			phsSrc.PrintInfo("1p");

			Mat mag = new Mat();
			Mat phs = new Mat();

			CvInvoke.Normalize(magSrc,mag,norm.MagMin,norm.MagMax,NormType.MinMax,DepthType.Cv32F);
			CvInvoke.Normalize(phsSrc,phs,norm.PhsMin,norm.PhsMax,NormType.MinMax,DepthType.Cv32F);

			mag.PrintInfo("2m");
			phs.PrintInfo("2p");

			Mat img;
			InverseChannel(mag,phs,out img);

			imgOut = new Mat();
			CvInvoke.Normalize(img,imgOut,0,65535,NormType.MinMax,DepthType.Cv16U);
		}

		static void InverseChannel(Mat mag, Mat phs, out Mat img)
		{
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
			img = compos[0];
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


		#if false
		//Followed this source code mostly
		//https://github.com/opencv-java/fourier-transform/blob/master/src/it/polito/teaching/cv/FourierController.java
		public static void DFTForward(DFTArgs args)
		{
			var imgSrc = CvInvoke.Imread(args.src,ImreadModes.Grayscale | ImreadModes.AnyDepth);

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

			string name = Path.GetFileNameWithoutExtension(args.dst);

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

			var magSrc = CvInvoke.Imread(magName,ImreadModes.Grayscale | ImreadModes.AnyDepth);
			var phsSrc = CvInvoke.Imread(phsName,ImreadModes.Grayscale | ImreadModes.AnyDepth);

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
		#endif
	}
}
