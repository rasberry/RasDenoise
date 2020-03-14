using System;
using System.Collections.Generic;

namespace RasDenoise
{
	public class NlMeansArgs
	{
		public string src;
		public string dst;
		public double h;
		public int templateWindowSize;
		public int searchWindowSize;

		public static NlMeansArgs Parse(string[] args)
		{
			var o = new NlMeansArgs();
			int len = args.Length;
			for(int a=0; a<len; a++)
			{
				string c = args[a];

				if (c == "-h" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.h,double.TryParse)) { return null; }
				} else if (c == "-t" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.templateWindowSize,int.TryParse)) { return null; }
				} else if (c == "-s" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.searchWindowSize,int.TryParse)) { return null; }
				} else {
					if (o.src == null) {
						o.src = args[a];
					} else if (o.dst == null) {
						o.dst = args[a];
					}
				}
			}
			return o;
		}
	}

	public class NlMeansColoredArgs
	{
		public string src;
		public string dst;
		public double h;
		public double hColor;
		public int templateWindowSize;
		public int searchWindowSize;

		public static NlMeansColoredArgs Parse(string[] args)
		{
			var o = new NlMeansColoredArgs();
			int len = args.Length;
			for(int a=0; a<len; a++)
			{
				string c = args[a];
				if (c == "-h" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.h,double.TryParse)) { return null; }
				} else if (c == "-t" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.templateWindowSize,int.TryParse)) { return null; }
				} else if (c == "-s" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.searchWindowSize,int.TryParse)) { return null; }
				} else {
					if (o.src == null) {
						o.src = args[a];
					} else if (o.dst == null) {
						o.dst = args[a];
					}
				}
			}
			return o;
		}
	}

	public class DctArgs
	{
		public string src;
		public string dst;
		public double? sigma;
		public int psize;

		public static DctArgs Parse(string[] args)
		{
			var o = new DctArgs();
			int len = args.Length;
			for(int a=0; a<len; a++)
			{
				string c = args[a];
				if (c == "-s" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.sigma,double.TryParse)) { return null; }
				} else if (c == "-p" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.psize,int.TryParse)) { return null; }
				}
			}
			return o;
		}
	}

	public class TVL1Args
	{
		public IEnumerable<string> srcList;
		public string dst;
		public double? lambda;
		public int? niters;

		public static TVL1Args Parse(string[] args)
		{
			var o = new TVL1Args();
			int len = args.Length;
			var fileList = new List<string>();
			for(int a=0; a<len; a++)
			{
				string c = args[a];
				if (c == "-o" && ++a < len) {
					o.dst = args[a];
				} else if (c == "-l" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.lambda,double.TryParse)) { return null; }
				} else if (c == "-n" && ++a < len) {
					if (!Helpers.TryParse(args[a],c,out o.niters,int.TryParse)) { return null; }
				} else {
					fileList.Add(args[a]);
				}
			}
			o.srcList = fileList;
			return o;
		}
	}

	public class DFTArgs
	{
		public string Phs;
		public string Mag;
		public string Dta;
		public string Ori;

		public static DFTArgs Parse(string[] args)
		{
			DFTArgs o = new DFTArgs();
			int len = args.Length;
			for(int a=0; a<len; a++)
			{
				string c = args[a];
				if (c == "-p" && ++a < len) { o.Phs = args[a]; }
				else if (c == "-m" && ++a < len) { o.Mag = args[a]; }
				else if (c == "-d" && ++a < len) { o.Dta = args[a]; }
				else if (all.Ori == null) { o.Ori = c; }
			}

			if (!String.IsNullOrEmpty(o.Ori))
			{
				string baseFile = Helpers.GetBaseName(o.Ori);
				if (all.Mag == null) { o.Mag = baseFile + ".mag.png"; }
				if (all.Phs == null) { o.Phs = baseFile + ".phs.png"; }
				if (all.Dta == null) { o.Dta = baseFile + ".dta.png"; }
			}
			return o;
		}
	}
}

#if false
static IArgs ParseArgs(string[] args)
{
	IArgs oargs = null;
	string m = args[0];
	if (m == "--help" || m == "/?" || m.EqualsIC("help") || m == "0") {
		MethodType meth = MethodType.Help;
		if (args.Length > 1) {
			if (!Enum.TryParse(args[1],true,out meth)) {
				meth = MethodType.Help;
			}
		}
		Usage(meth);
		return oargs;
	}

	if (!Enum.TryParse(m,true,out Method)) {
		Console.WriteLine("Bad method "+m);
		return oargs;
	}

	int len = args.Length;
	if (Method == MethodType.NlMeans) {
		var fargs = new NlMeansArgs();

		for(int a=1; a<len; a++)
		{
			string c = args[a];

			if (c == "-h" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out fargs.h,double.TryParse)) { return false; }
			} else if (c == "-t" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out fargs.templateWindowSize,int.TryParse)) { return false; }
			} else if (c == "-s" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out fargs.searchWindowSize,int.TryParse)) { return false; }
			} else {
				if (fargs.src == null) {
					fargs.src = args[a];
				} else if (fargs.dst == null) {
					fargs.dst = args[a];
				}
			}
			oargs = fargs;
		}
		else if (Method == MethodType.NlMeansColored) {
			if (c == "-h" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m2h,double.TryParse)) { return false; }
			} else if (c == "-t" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m2templateWindowSize,int.TryParse)) { return false; }
			} else if (c == "-s" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m2searchWindowSize,int.TryParse)) { return false; }
			} else if (c == "-c" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m2hColor,double.TryParse)) { return false; }
			} else {
				if (fargs.src == null) {
					fargs.src = args[a];
				} else if (fargs.dst == null) {
					fargs.dst = args[a];
				}
			}
		}
		else if (Method == MethodType.Dct) {
			if (c == "-s" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m3sigma,double.TryParse)) { return false; }
			} else if (c == "-p" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m3psize,int.TryParse)) { return false; }
			}
		}
		else if (Method == MethodType.TVL1) {
			if (c == "-o" && ++a < len) {
				outFile = args[a];
			} else if (c == "-l" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m4lambda,double.TryParse)) { return false; }
			} else if (c == "-n" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m4niters,int.TryParse)) { return false; }
			} else {
				fileList.Add(args[a]);
			}
		}
		else if (Method == MethodType.DFTForward) {
			if (c == "-o" && ++a < len) {
				outFile = args[a];
			} else {
				fileList.Add(args[a]);
			}
		}
		else if (Method == MethodType.DFTInverse) {
			if (c == "-m" && ++a < len) {
				if (fileList.Count < 1) {
					fileList.Add(args[a]);
				} else {
					fileList[0] = args[a];
				}
			} else if (c == "-p" && ++a < len) {
				if (fileList.Count < 1) {
					fileList.Add(null);
				}
				fileList.Add(args[a]);
			} else if (c == "-mi" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m6mi, double.TryParse)) { return false; }
			} else if (c == "-mx" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m6mx, double.TryParse)) { return false; }
			} else if (c == "-pi" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m6pi, double.TryParse)) { return false; }
			} else if (c == "-px" && ++a < len) {
				if (!Helpers.TryParse(args[a],c,out m6px, double.TryParse)) { return false; }
			} else {
				outFile = args[a];
			}
		}
	}
	return oargs;
}
#endif
