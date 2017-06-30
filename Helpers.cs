using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RasDenoise
{
	static class Helpers
	{
		public static StringBuilder AppendWrap(this StringBuilder self, int offset, string m)
		{
			int w = Console.BufferWidth - 1 - offset;
			int c = 0;
			int l = m.Length;

			while(c < l) {
				//need spacing after first line
				string o = c > 0 ? new string(' ',offset) : "";
				//this is the last iteration
				if (c + w >= l) {
					string s = m.Substring(c);
					c += w;
					self.Append(o).AppendLine(s);
				}
				//were in the middle
				else {
					string s = m.Substring(c,w);
					c += w;
					self.Append(o).AppendLine(s);
				}
			}
			//StringBuilder like to chain
			return self;
		}

		public static bool EqualsIC(this string self, string check)
		{
			return self != null && check != null && self.Equals(check,StringComparison.CurrentCultureIgnoreCase);
		}

		//http://stackoverflow.com/questions/2961656/generic-tryparse
		public delegate bool TryParseHandler<T>(string value, out T result);
		public static bool TryParse<T>(string sval, string name, out T val, TryParseHandler<T> handler) where T : struct
		{
			bool worked = handler(sval,out val);
			if (!worked) {
				Console.WriteLine("Could not parse "+name+" value "+sval);
			}
			return worked;
		}
		public static bool TryParse<T>(string sval, string name, out T? val, TryParseHandler<T> handler) where T : struct
		{
			val = new T?();
			bool worked = handler(sval, out T outval);
			if (!worked) {
				Console.WriteLine("Could not parse "+name+" value "+sval);
			} else {
				val = outval;
			}
			return worked;
		}

		public static T GetValue<T>(this Mat mat, int row, int col)
		{
			var buffer = new byte[mat.ElementSize];
			IntPtr src = mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize;
			Marshal.Copy(src, buffer, 0, mat.ElementSize);
			return mat.ConvertFrom<T>(buffer);
		}

		static T ConvertFrom<T>(this Mat mat, byte[] buffer)
		{
			DepthType depth = mat.Depth;
			if (depth == DepthType.Cv8S)  {
				var tmp = (sbyte)buffer[0];
				return (T)((object)tmp);
			}
			else if (depth == DepthType.Cv8U)  {
				var tmp = (byte)buffer[0];
				return (T)((object)tmp);
			}
			else if (depth == DepthType.Cv16S) {
				var tmp = (short)BitConverter.ToInt16(buffer,0);
				return (T)((object)tmp);
			}
			else if (depth == DepthType.Cv16U) {
				var tmp = (ushort)BitConverter.ToInt16(buffer,0);
				return (T)((object)tmp);
			}
			else if (depth == DepthType.Cv32S) {
				var tmp = (int)BitConverter.ToInt32(buffer,0);
				return (T)((object)tmp);
			}
			else if (depth == DepthType.Cv64F) {
				var tmp = (double)BitConverter.ToDouble(buffer,0);
				return (T)((object)tmp);
			}
			else {
				//if (depthType == DepthType.Cv32F) {
				var tmpx = (float)BitConverter.ToSingle(buffer,0);
				return (T)((object)tmpx);
			}
		}

		public static bool IsCompatible<T>(this DepthType depthType, T val)
		{
			if (depthType == DepthType.Cv8S)  { return val is sbyte; }
			if (depthType == DepthType.Cv8U)  { return val is byte; }
			if (depthType == DepthType.Cv16S) { return val is short; }
			if (depthType == DepthType.Cv16U) { return val is ushort; }
			if (depthType == DepthType.Cv32S) { return val is int; }
			if (depthType == DepthType.Cv32F) { return val is float; }
			if (depthType == DepthType.Cv64F) { return val is double; }
			return val is float;
		}

		public static void SetValue<T>(this Mat mat, int row, int col, T value)
		{
			byte[] buffer = null;
			try {
				buffer = mat.ConvertTo<T>(value);
			} catch(Exception e) {
				Console.WriteLine(e.Message+" SetValue r="+row+" c="+col+" value="+value+" "+mat.Depth);
				return;
			}
			
			IntPtr src = mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize;
			Marshal.Copy(buffer, 0, src, mat.ElementSize);
		}

		static byte[] ConvertTo<T>(this Mat mat, T value)
		{
			byte[] buffer;
			DepthType depth = mat.Depth;

			if (depth == DepthType.Cv8S)  {
				buffer = new byte[mat.ElementSize];
				buffer[0] = (byte)Convert.ChangeType(value,typeof(sbyte));
			}
			else if (depth == DepthType.Cv8U)  {
				buffer = new byte[mat.ElementSize];
				buffer[0] = (byte)Convert.ChangeType(value,typeof(byte));
			}
			else if (depth == DepthType.Cv16S) {
				short tmp = (short)Convert.ChangeType(value,typeof(short));
				buffer = BitConverter.GetBytes(tmp);
			}
			else if (depth == DepthType.Cv16U) {
				ushort tmp = (ushort)Convert.ChangeType(value,typeof(ushort));
				buffer = BitConverter.GetBytes(tmp);
			}
			else if (depth == DepthType.Cv32S) {
				int tmp = (int)Convert.ChangeType(value,typeof(int));
				buffer = BitConverter.GetBytes(tmp);
			}
			else if (depth == DepthType.Cv64F) {
				double tmp = (double)Convert.ChangeType(value,typeof(double));
				buffer = BitConverter.GetBytes(tmp);
			}
			else {
				//if (depthType == DepthType.Cv32F) {
				float tmp = (float)Convert.ChangeType(value,typeof(float));
				buffer = BitConverter.GetBytes(tmp);
			}
			return buffer;
		}

		#if false
		public static dynamic GetValue(this Mat mat, int row, int col)
		{
			var value = CreateElement(mat.Depth);
			Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
			return value[0];
		}

		public static void SetValue(this Mat mat, int row, int col, dynamic value)
		{
			var target = CreateElement(mat.Depth, value);
			Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
		}
		private static dynamic CreateElement(DepthType depthType, dynamic value)
		{
			var element = CreateElement(depthType);
			element[0] = value;
			return element;
		}

		private static dynamic CreateElement(DepthType depthType)
		{
			if (depthType == DepthType.Cv8S)
			{
				return new sbyte[1];
			}
			if (depthType == DepthType.Cv8U)
			{
				return new byte[1];
			}
			if (depthType == DepthType.Cv16S)
			{
				return new short[1];
			}
			if (depthType == DepthType.Cv16U)
			{
				return new ushort[1];
			}
			if (depthType == DepthType.Cv32S)
			{
				return new int[1];
			}
			if (depthType == DepthType.Cv32F)
			{
				return new float[1];
			}
			if (depthType == DepthType.Cv64F)
			{
				return new double[1];
			}
			return new float[1];
		}
		#endif

		public static double BytesToDouble(byte[] bytes)
		{
			int len = bytes.Length;
			if (len == 0) {
				return 0.0;
			}
			if (len == 1) {
				return bytes[0];
			}
			if (len == 2) {
				return BitConverter.ToInt16(bytes,0);
			}
			if (len == 4) {
				return BitConverter.ToInt32(bytes,0);
			}
			if (len == 8) {
				return BitConverter.ToInt64(bytes,0);
			}

			throw new NotSupportedException("Unsupported length "+bytes.Length);
		}

		public static void PrintInfo(this Mat self, string name = null)
		{
			double min,max;
			CvInvoke.MinMaxIdx(self,out min,out max,null,null);

			Console.WriteLine("Mat: " + (name == null ? "" : name)
				+"\n\tDepth="+self.Depth
				+"\n\tChannels="+self.NumberOfChannels
				+"\n\tSize="+self.Size.Width+"x"+self.Size.Height
				+"\n\tDims="+self.Dims
				+"\n\tElementSize="+self.ElementSize
				+"\n\tMin="+min
				+"\n\tMax="+max
			);
		}

		////Don't know why this isn't exposed .. cv::Magnitude
		////ah ha! -- cartToPolar does this
		//public static void Magnitude(IInputArray x,IInputArray y, IOutputArray magnitude)
		//{
		//	Mat xpow = new Mat();
		//	Mat ypow = new Mat();
		//
		//	CvInvoke.Pow(x,2.0,xpow);
		//	CvInvoke.Pow(y,2.0,ypow);
		//	CvInvoke.Add(xpow,ypow,magnitude);
		//}

		//Not sure why cv:AddS went away
		public static void AddS(Mat input, double amount, IOutputArray output)
		{
			Mat scale = new Mat(input.Size,input.Depth,1);
			scale.SetTo(new MCvScalar(amount));
			CvInvoke.Add(input,scale,output);
		}
	}
}
