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

		#if false //works
		public static void FFTForward(string src, string dst)
		{
			var imgSrc = CvInvoke.Imread(src,LoadImageType.Grayscale);

			int xdftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Rows);
			int ydftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Cols);

			var imgPad = new Mat();
			CvInvoke.CopyMakeBorder(imgSrc,imgPad,
				0,xdftsz - imgSrc.Rows,0,ydftsz - imgSrc.Cols,
				BorderType.Constant,new MCvScalar(0)
			);

			var complex = new ComplexArray2D(imgPad.Width,imgPad.Height);

			for(int y=0; y<imgPad.Height; y++) {
				for(int x=0; x<imgPad.Width; x++) {
					var dt = imgPad.GetData(x,y);
					double val = Helpers.BytesToDouble(dt);
					complex[x,y] = new System.Numerics.Complex(val,0.0);
				}
			}

			Fft.Transform2D(complex,true);

			Mat imgMag = new Mat(imgPad.Size,DepthType.Cv8U,1);
			Mat imgPhs = new Mat(imgPad.Size,DepthType.Cv8U,1);

			double magmax = double.MinValue;
			double magmin = double.MaxValue;
			double phsmax = double.MinValue;
			double phsmin = double.MaxValue;

			for(int y=0; y<imgPad.Height; y++) {
				for(int x=0; x<imgPad.Width; x++) {
					Complex c = complex[x,y];
					double mago = c.Magnitude;
					double mag = Math.Log(mago);
					double phs = c.Phase;
					if (mag > magmax) { magmax = mag; }
					if (mag < magmin) { magmin = mag; }
					if (phs > phsmax) { phsmax = phs; }
					if (phs < phsmin) { phsmin = phs; }
				}
			}

			double magdiff = magmax - magmin;
			double phsdiff = phsmax - phsmin;
			double smax = (double)byte.MaxValue;

			Console.WriteLine("magmin="+magmin+" magmax="+magmax+" magdiff="+magdiff);
			Console.WriteLine("phsmin="+phsmin+" phsmax="+phsmax+" phsdiff="+phsdiff);

			for(int y=0; y<imgPad.Height; y++) {
				for(int x=0; x<imgPad.Width; x++) {
					Complex c = complex[x,y];
					double mago = c.Magnitude;
					double mag = Math.Log(mago);
					double phs = c.Phase;
					
					byte smag = (byte)((mag - magmin)*(smax / magdiff));
					byte sphs = (byte)((phs - phsmin)*(smax / phsdiff));

					imgMag.SetValue(y,x,smag);
					imgPhs.SetValue(y,x,sphs);
				}
			}

			string name = Path.GetFileNameWithoutExtension(dst);

			imgMag.Save(name+"-mag.png");
			imgPhs.Save(name+"-phs.png");

			//CvInvoke.Imshow("test1", imgMag); //Show the image
			//CvInvoke.WaitKey(0);  //Wait for the key pressing event
			//CvInvoke.DestroyWindow("test1"); //Destroy the window if key is pressed
		}
		#endif

		#if false
		public static void FFTForward(string src, string dst)
		{
			// Load image
			Image<Gray, float> image = new Image<Gray, float>(src);

			// Transform 1 channel grayscale image into 2 channel image
			IntPtr complexImage = CvInvoke.cvCreateImage(image.Size, IplDepth.IplDepth32F, 2);
			CvInvoke.cvSetImageCOI(complexImage, 1); // Select the channel to copy into
			CvInvoke.cvCopy(image, complexImage, IntPtr.Zero);
			CvInvoke.cvSetImageCOI(complexImage, 0); // Select all channels

			var complexMat = CvInvoke.CvArrToMat(complexImage);
			// This will hold the DFT data
			Matrix<float> forwardDft = new Matrix<float>(image.Rows, image.Cols, 2); 
			CvInvoke.Dft(complexMat, forwardDft, DxtType.Forward, 0);

			CvInvoke.cvReleaseImage(ref complexImage);

			// We'll display the magnitude
			Matrix<float> forwardDftMagnitude = GetDftMagnitude(forwardDft); 
			SwitchQuadrants(ref forwardDftMagnitude); 

			// Now compute the inverse to see if we can get back the original
			Matrix<float> reverseDft = new Matrix<float>(forwardDft.Rows, forwardDft.Cols, 2);
			CvInvoke.Dft(forwardDft, reverseDft, DxtType.Inverse, 0);
			Matrix<float> reverseDftMagnitude = GetDftMagnitude(reverseDft);    

			//pictureBox1.Image = image.ToBitmap();
			//pictureBox2.Image = Matrix2Bitmap(forwardDftMagnitude);
			//pictureBox3.Image = Matrix2Bitmap(reverseDftMagnitude);
		}

		//static Bitmap Matrix2Bitmap(Matrix<float> matrix)
		//{
		//	CvInvoke.cvNormalize(matrix, matrix, 0.0, 255.0, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);            
		//
		//	Image<Gray, float> image = new Image<Gray, float>(matrix.Size);
		//	matrix.CopyTo(image);
		//
		//	return image.ToBitmap();
		//}

		// Real part is magnitude, imaginary is phase. 
		// Here we compute log(sqrt(Re^2 + Im^2) + 1) to get the magnitude and 
		// rescale it so everything is visible
		static Matrix<float> GetDftMagnitude(Matrix<float> fftData)
		{
			//The Real part of the Fourier Transform
			Matrix<float> outReal = new Matrix<float>(fftData.Size);
			//The imaginary part of the Fourier Transform
			Matrix<float> outIm = new Matrix<float>(fftData.Size);
			CvInvoke.Split(fftData,
			CvInvoke.cvSplit(fftData, outReal, outIm, IntPtr.Zero, IntPtr.Zero);

			CvInvoke.cvPow(outReal, outReal, 2.0);
			CvInvoke.cvPow(outIm, outIm, 2.0);

			CvInvoke.cvAdd(outReal, outIm, outReal, IntPtr.Zero);
			CvInvoke.cvPow(outReal, outReal, 0.5);

			CvInvoke.cvAddS(outReal, new MCvScalar(1.0), outReal, IntPtr.Zero); // 1 + Mag
			CvInvoke.cvLog(outReal, outReal); // log(1 + Mag)            


			return outReal;
		}

		// We have to switch quadrants so that the origin is at the image center
		static void SwitchQuadrants(ref Matrix<float> matrix)
		{
			int cx = matrix.Cols / 2;
			int cy = matrix.Rows / 2;

			Matrix<float> q0 = matrix.GetSubRect(new Rectangle(0, 0, cx, cy));
			Matrix<float> q1 = matrix.GetSubRect(new Rectangle(cx, 0, cx, cy));
			Matrix<float> q2 = matrix.GetSubRect(new Rectangle(0, cy, cx, cy));
			Matrix<float> q3 = matrix.GetSubRect(new Rectangle(cx, cy, cx, cy));
			Matrix<float> tmp = new Matrix<float>(q0.Size);

			q0.CopyTo(tmp);
			q3.CopyTo(q0);
			tmp.CopyTo(q3);
			q1.CopyTo(tmp);
			q2.CopyTo(q1);
			tmp.CopyTo(q2);
		}
		#endif

		#if false
		public static void FFTForward(string src, string dst)
		{
			var imgSrc = CvInvoke.Imread(src,LoadImageType.Grayscale);
			int xdftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Rows);
			int ydftsz = CvInvoke.GetOptimalDFTSize(imgSrc.Cols);

			var imgPad = new Mat();
			CvInvoke.CopyMakeBorder(imgSrc,imgPad,
				0,xdftsz - imgSrc.Rows,0,ydftsz - imgSrc.Cols,
				BorderType.Constant,new MCvScalar(0)
			);
			
			Mat imgFlt = new Mat(imgPad.Size,DepthType.Cv32F,1);
			imgPad.CopyTo(imgFlt);
			
			var planes = new VectorOfMat();
			planes.Push(imgFlt);
			planes.Push(new Mat(imgPad.Size,DepthType.Cv32F,1));
			var complex = new Mat(imgPad.Size,DepthType.Cv32F,2);
			CvInvoke.Merge(planes,complex);

			Matrix<double> dftMx = new Matrix<double>(imgPad.Rows,imgPad.Cols,2);
			CvInvoke.Dft(complex,dftMx,DxtType.Forward,0);

			CvInvoke.Split(complex,planes);
			CudaInvoke.Magnitude(planes[0],planes[1],planes[0]);

			Mat imgMag = planes[0];
			CvInvoke.Imshow("planes0",imgMag);
			CvInvoke.WaitKey();

			//Mat magI = planes[0];
			//magI += MCvScalar(0);
		}
		#endif

		public static void Test()
		{
			var x = new Emgu.CV.Superres.FrameSource(null,false);
			var y = new Emgu.CV.Superres.SuperResolution(Emgu.CV.Superres.SuperResolution.OpticalFlowType.Btvl,x);
		}
	}
}
