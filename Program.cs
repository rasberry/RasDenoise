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
			AppDomain appCurr = AppDomain.CurrentDomain;
			appCurr.AssemblyResolve += (object sender, ResolveEventArgs eventArgs) => {
				Console.WriteLine("## "+eventArgs.Name);
				return null;
			};
			appCurr.AssemblyLoad += (object sender, AssemblyLoadEventArgs eventArgs) => {
				Console.WriteLine("!! "+eventArgs.LoadedAssembly.FullName);
			};

			if (args.Length < 1) {
				Usage();
				return;
			}
			if (!ParseMethod(args)) {
				return;
			}

			//windows pops up a dialog on a crash - using try-catch to suppress that
			try {
				MainMain(args.Skip(1).ToArray());
			} catch(SEHException se) {
				//SEHException is annoying .. not sure how to get the 'native' underlying crash information yet.
				Console.Error.WriteLine(se.ToString());
			} catch(Exception e) {
				Console.Error.WriteLine(e.ToString());
			}
		}

		enum MethodType { None = -1, Help=0, NlMeans=1, NlMeansColored=2, Dct=3, TVL1=4, DFTForward=5, DFTInverse=6 }
		static MethodType Method = MethodType.None;

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
					.AppendLine(" Additional Information:")
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
					.AppendLine("  - forward outputs 3 files:")
					.AppendLine("    basename.mag - magnitude part")
					.AppendLine("    basename.phs - phase part")
					.AppendLine("    basename.dta - text file with original magnitude and phase ranges")
					.Append    ("  -m (file)             ").AppendWrap(24,"specify magnitude file to write")
					.Append    ("  -p (file)             ").AppendWrap(24,"specify phase file to write")
					.Append    ("  -d (file)             ").AppendWrap(24,"specify data file to write")
			;}
			if (help == MethodType.Help || help == MethodType.DFTInverse) {
				sb
					.AppendLine()
					.AppendLine("  [6] DFTInverse")
					.Append    ("  ").AppendWrap(2,"Recomposes an image from frequency magnitude and phase components.")
					.AppendLine()
					.Append    ("  (input base name)    ").AppendWrap(24,"input base file name")
					.Append    ("  ").AppendWrap(2,"- inverse expects (basename).mag, .phs, and .dta files to exist in the same folder as basename")
					.Append    ("  -m (file)             ").AppendWrap(24,"specify magnitude file to use")
					.Append    ("  -p (file)             ").AppendWrap(24,"specify phase file to use")
					.Append    ("  -d (file)             ").AppendWrap(24,"specify data file to use")
			;}
			}

			Console.WriteLine(sb.ToString());
		}

		static bool ParseMethod(string[] args)
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
				return true;
			}

			return true;
		}

		static void MainMain(string[] args)
		{
			//not sure how beneficial this is
			CvInvoke.RedirectError(CvInvoke.CvErrorHandlerIgnoreError,IntPtr.Zero,IntPtr.Zero);

			//string inFile = null;
			//inFile = fileList.FirstOrDefault();
			//if (String.IsNullOrWhiteSpace(inFile)) {
			//	Console.WriteLine("Missing input file");
			//	return;
			//}
			//if(String.IsNullOrEmpty(outFile))
			//{
			//	if (fileList.Count > 1) {
			//		outFile = fileList[1];
			//	} else {
			//		outFile = Path.GetFileNameWithoutExtension(inFile)
			//			+".out"+Path.GetExtension(inFile);
			//	}
			//}

			if (Method == MethodType.NlMeans) {
				var ments = NlMeansArgs.Parse(args);
				Methods.NlMeans(ments);
			}
			else if (Method == MethodType.NlMeansColored) {
				var ments = NlMeansColoredArgs.Parse(args);
				Methods.NlMeansColored(ments);
			}
			else if (Method == MethodType.Dct) {
				var ments = DctArgs.Parse(args);
				if (!ments.sigma.HasValue) {
					Console.WriteLine("option -s is required");
					return;
				}
				Methods.Dct(ments);
			}
			else if (Method == MethodType.TVL1) {
				var ments = TVL1Args.Parse(args);
				if (!ments.lambda.HasValue) {
					Console.WriteLine("option -l is required");
					return;
				}
				if (!ments.niters.HasValue) {
					Console.WriteLine("option -n is required");
					return;
				}
				Methods.TVL1(ments);
			}
			else if (Method == MethodType.DFTForward) {
				var ments = DFTArgs.Parse(args);
				Methods.DFTForward(ments);
			}
			//else if (Method == MethodType.DFTInverse) {
			//	if (fileList.Count < 2 || fileList[0] == null || fileList[1] == null) {
			//		Console.WriteLine("your must specify both a magnitude image and a phase image");
			//		return;
			//	}
			//	if (fileList.Count > 2) {
			//		outFile = fileList[2];
			//	} else {
			//		outFile = Path.GetFileNameWithoutExtension(fileList[0])+".inv.png";
			//	}
			//
			//	Debug.WriteLine("mi="+m6mi.GetValueOrDefault()
			//		+" mx="+m6mx.GetValueOrDefault()
			//		+" pi="+m6pi.GetValueOrDefault()
			//		+" px="+m6px.GetValueOrDefault()
			//	);
			//
			//	if (!m6mi.HasValue || !m6mx.HasValue || !m6pi.HasValue || !m6px.HasValue) {
			//		Console.WriteLine("you must specify magnitude and phase ranges");
			//		return;
			//	}
			//
			//	Methods.DFTInverse(fileList,outFile,m6mi.Value,m6mx.Value,m6pi.Value,m6px.Value);
			//	return;
			//}
		}
	}
}
