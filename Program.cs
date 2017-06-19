using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RasDenoise
{
	class Program
	{
		static void Main(string[] args)
		{
			Debug.Listeners.Add(new ConsoleTraceListener());

			if (args.Length < 1) {
				Usage();
				return;
			}
			if (!ParseArgs(args)) {
				return;
			}

			//windows pops up a dialog on a crash - using try-catch to suppress that
			try {
				MainMain();
			} catch(SEHException se) {
				//SEHException is annoying .. not sure how to get the 'native' underlying crash information yet.
				Console.Error.WriteLine(se.ToString());
			} catch(Exception e) {
				Console.Error.WriteLine(e.ToString());
			}
		}

		enum MethodType { None = -1, Help=0, NlMeans=1, NlMeansColored=2, Dct=3, TVL1=4, DFTForward=5, DFTInverse=6 }

		//http://www.emgu.com/wiki/files/3.0.0/document/html/a4912b79-7f4b-b67a-1822-08e0bff036bd.htm
		static void Usage(MethodType help = MethodType.None)
		{
			StringBuilder sb = new StringBuilder();

			sb
				.AppendLine("Usage: "+nameof(RasDenoise)+" (method) [options] [file1 file2 ...]")
				.AppendLine(" Methods: ")
				.AppendLine("  [0] Help                  Show full help")
				.AppendLine("  [1] NlMeans               Denoising using Non-local Means algorithm")
				.AppendLine("  [2] NlMeansColored        Denoising using Non-local Means algorithm (modified for color)")
				.AppendLine("  [3] Dct                   Simple dct-based denoising")
				.AppendLine("  [4] TVL1                  Denoising via primal-dual algorithm")
				.AppendLine("  [5] DFTForward            Transform image using forward fourier transform")
				.AppendLine("  [6] DFTInverse            Transform image(s) using inverse fourier transform")
			;

			if (help >= MethodType.Help) {
				sb
					.AppendLine()
					.AppendLine("  Additional Information:")
				;
			if (help == MethodType.Help || help == MethodType.NlMeans) {
				sb
					.AppendLine()
					.AppendLine("  [1] NlMeans")
					.Append    ("  ").AppendWrap(2,"Perform image denoising using Non-local Means Denoising algorithm: http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise.")
					.AppendLine()
					.Append    ("  (input image)         ").AppendWrap(24,"Input 8-bit 1-channel, 2-channel or 3-channel image.")
					.Append    ("  [output image]        ").AppendWrap(24,"Output image")
					.Append    ("  -h [float = 3.0]      ").AppendWrap(24,"Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.")
					.Append    ("  -t [int = 7]          ").AppendWrap(24,"Size in pixels of the template patch that is used to compute weights. Should be odd.")
					.Append    ("  -s [int = 21]         ").AppendWrap(24,"Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affect performance linearly: greater searchWindowsSize - greater denoising time.")
			;}
			if (help == MethodType.Help || help == MethodType.NlMeansColored) {
				sb
					.AppendLine()
					.AppendLine("  [2] NlMeansColored")
					.Append    ("  ").AppendWrap(2,"Perform image denoising using Non-local Means Denoising algorithm (modified for color image): http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise. The function converts image to CIELAB colorspace and then separately denoise L and AB components with given h parameters using fastNlMeansDenoising function.")
					.AppendLine()
					.Append    ("  (input image)         ").AppendWrap(24,"Input 8-bit 1-channel, 2-channel or 3-channel image.")
					.Append    ("  [output image]        ").AppendWrap(24,"Output image")
					.Append    ("  -h [float = 3.0]      ").AppendWrap(24,"Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.")
					.Append    ("  -c [float = 3.0]      ").AppendWrap(24,"The same as -h but for color components. For most images value equals 10 will be enought to remove colored noise and do not distort colors.")
					.Append    ("  -t [int = 7]          ").AppendWrap(24,"Size in pixels of the template patch that is used to compute weights. Should be odd.")
					.Append    ("  -s [int = 21]         ").AppendWrap(24,"Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affect performance linearly: greater searchWindowsSize - greater denoising time.")
			;}
			if (help == MethodType.Help || help == MethodType.Dct) {
				sb
					.AppendLine()
					.AppendLine("  [3] Dct")
					.Append    ("  ").AppendWrap(2,"The function implements simple dct-based denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.")
					.AppendLine()
					.Append    ("  (input image)         ").AppendWrap(24,"Source image")
					.Append    ("  [output image]        ").AppendWrap(24,"Output image")
					.Append    ("  -s (double)           ").AppendWrap(24,"Expected noise standard deviation")
					.Append    ("  -p [int = 16]         ").AppendWrap(24,"Size of block side where dct is computed")
			;}
			if (help == MethodType.Help || help == MethodType.TVL1) {
				sb
					.AppendLine()
					.AppendLine("  [4] TVL1")
					.Append    ("  ").AppendWrap(2,"The function implements simple dct-based denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.")
					.AppendLine()
					.Append    ("  (file) [file] [...]   ").AppendWrap(24,"One or more noised versions of the image that is to be restored.")
					.Append    ("  -o [output image]     ").AppendWrap(24,"Output image")
					.Append    ("  -l (double)           ").AppendWrap(24,"Corresponds to in the formulas above. As it is enlarged, the smooth (blurred) images are treated more favorably than detailed (but maybe more noised) ones. Roughly speaking, as it becomes smaller, the result will be more blur but more sever outliers will be removed.")
					.Append    ("  -n (int)              ").AppendWrap(24,"Number of iterations that the algorithm will run. Of course, as more iterations as better, but it is hard to quantitatively refine this statement, so just use the default and increase it if the results are poor.")
			;}
			if (help == MethodType.Help || help == MethodType.DFTForward) {
				sb
					.AppendLine()
					.AppendLine("  [5] DFTForward")
					.Append    ("  ").AppendWrap(2,"Decomposes an input image into frequency magnitude and phase components.")
					.AppendLine()
					.Append    ("  (input image)         ").AppendWrap(24,"Source image")
					.Append    ("  -m [output image]     ").AppendWrap(24,"Output magnitude image")
					.Append    ("  -p [output image]     ").AppendWrap(24,"Output phase image")
			;}
			if (help == MethodType.Help || help == MethodType.DFTInverse) {
				sb
					.AppendLine()
					.AppendLine("  [5] DFTInverse")
					.Append    ("  ").AppendWrap(2,"Recomposes an image from frequency magnitude and phase components.")
					.AppendLine()
					.Append    ("  (output image)       ").AppendWrap(24,"Output image")
					.Append    ("  -m (input image)     ").AppendWrap(24,"Input magnitude image")
					.Append    ("  -p (input image)     ").AppendWrap(24,"Input phase image")
			;}
			}

			Console.WriteLine(sb.ToString());
		}

		//options variables and their defaults
		static List<string> fileList = new List<string>();
		static string outFile = null;
		static MethodType Method = MethodType.None;
		static double  m1h = 3.0;
		static int     m1templateWindowSize = 7;
		static int     m1searchWindowSize = 21;
		static double  m2h = 3.0;
		static double  m2hColor = 3.0;
		static int     m2templateWindowSize = 7;
		static int     m2searchWindowSize = 21;
		static double? m3sigma;
		static int     m3psize = 16;
		static double? m4lambda;
		static int?    m4niters;

		static bool ParseArgs(string[] args)
		{
			string m = args[0];
			if (m == "--help" || m == "/?" || m.EqualsIC("help") || m == "0") {
				MethodType meth = MethodType.Help;
				if (args.Length > 1) {
					if (!Enum.TryParse(args[1],true,out meth)) {
						meth = MethodType.Help;
					}
				}
				Usage(meth);
				return false;
			}

			if (!Enum.TryParse(m,true,out Method)) {
				Console.WriteLine("Bad method "+m);
				return false;
			}

			int len = args.Length;
			for(int a=1; a<len; a++)
			{
				string c = args[a];
				if (c == "-h" && ++a < len) {
					if (Method == MethodType.NlMeans) {
						if (!Helpers.TryParse(args[a],c,out m1h,double.TryParse)) { return false; }
					} else if (Method == MethodType.NlMeansColored) {
						if (!Helpers.TryParse(args[a],c,out m2h,double.TryParse)) { return false; }
					}
				}
				else if (c == "-t" && ++a < len) {
					if (Method == MethodType.NlMeans) {
						if (!Helpers.TryParse(args[a],c,out m1templateWindowSize,int.TryParse)) { return false; }
					} else if (Method == MethodType.NlMeansColored) {
						if (!Helpers.TryParse(args[a],c,out m2templateWindowSize,int.TryParse)) { return false; }
					}
				} else if (c == "-s" && ++a < len) {
					if (Method == MethodType.NlMeans) {
						if (!Helpers.TryParse(args[a],c,out m1searchWindowSize,int.TryParse)) { return false; }
					} else if (Method == MethodType.NlMeansColored) {
						if (!Helpers.TryParse(args[a],c,out m2searchWindowSize,int.TryParse)) { return false; }
					} else if (Method == MethodType.Dct) {
						if (!Helpers.TryParse(args[a],c,out m3sigma,double.TryParse)) { return false; }
					}
				} else if (c == "-c" && ++a < len) {
					if (Method == MethodType.NlMeansColored) {
						if (!Helpers.TryParse(args[a],c,out m2hColor,double.TryParse)) { return false; }
					}
				} else if (c == "-p" && ++a < len) {
					if (Method == MethodType.Dct) {
						if (!Helpers.TryParse(args[a],c,out m3psize,int.TryParse)) { return false; }
					}
				} else if (c == "-o" && ++a < len) {
					if (Method == MethodType.TVL1) {
						outFile = args[a];
					}
				} else if (c == "-l" && ++a < len) {
					if (Method == MethodType.TVL1) {
						if (!Helpers.TryParse(args[a],c,out m4lambda,double.TryParse)) { return false; }
					}
				} else if (c == "-n" && ++a < len) {
					if (Method == MethodType.TVL1) {
						if (!Helpers.TryParse(args[a],c,out m4niters,int.TryParse)) { return false; }
					}
				} else {
					fileList.Add(args[a]);
				}
			}
			return true;
		}

		static void MainMain()
		{
			//not sure how beneficial this is
			CvInvoke.RedirectError(CvInvoke.CvErrorHandlerIgnoreError,IntPtr.Zero,IntPtr.Zero);

			string inFile = null;
			inFile = fileList.FirstOrDefault();
			if (String.IsNullOrWhiteSpace(inFile)) {
				Console.WriteLine("Missing input file");
				return;
			}
			if(String.IsNullOrEmpty(outFile))
			{
				if (fileList.Count > 1) {
					outFile = fileList[1];
				} else {
					outFile = Path.GetFileNameWithoutExtension(inFile)
						+".dn"+Path.GetExtension(inFile);
				}
			}

			if (Method == MethodType.NlMeans) {
				Methods.NlMeans(inFile,outFile,m1h,m1templateWindowSize,m1searchWindowSize);
			}
			else if (Method == MethodType.NlMeansColored) {
				Methods.NlMeansColored(inFile,outFile,m2h,m2hColor,m2templateWindowSize,m2searchWindowSize);
			}
			else if (Method == MethodType.Dct) {
				if (!m3sigma.HasValue) {
					Console.WriteLine("option -s is required");
					return;
				}
				Methods.Dct(inFile,outFile,m3sigma.Value,m3psize);
			}
			else if (Method == MethodType.TVL1) {
				if (!m4lambda.HasValue) {
					Console.WriteLine("option -l is required");
					return;
				}
				if (!m4niters.HasValue) {
					Console.WriteLine("option -n is required");
					return;
				}
				Methods.TVL1(fileList,outFile,m4lambda.Value,m4niters.Value);
			}
			else if (Method == MethodType.DFTForward) {
				Methods.DFTForward(inFile,outFile);
				return;
			}
			else if (Method == MethodType.DFTInverse) {
				Methods.DFTInverse(fileList,outFile);
				return;
			}
		}
	}
}
