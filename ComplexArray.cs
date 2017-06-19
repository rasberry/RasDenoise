using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RasDenoise
{
	public interface IComplexArray
	{
		Complex this[int i] { get; set; }
		int Length { get; }
	}

	public static class ComplexArrayHelpers
	{
		public static IComplexArray Clone(this IComplexArray input)
		{
			IComplexArray output = new ComplexArray(input.Length);
			for(int i=0; i<input.Length; i++) {
				output[i] = input[i];
			}
			return output;
		}
	}

	public class ComplexArray : IComplexArray
	{
		Complex[] array;

		public ComplexArray(int len)
		{
			array = new Complex[len];
		}

		public int Length { get { return array.Length; } }

		public Complex this[int i] {
			get {
				return array[i];
			}
			set {
				array[i] = value;
			}
		}
	}

	public class ComplexArray2D
	{
		Complex[,] array;

		public ComplexArray2D(int width,int height)
		{
			array = new Complex[width,height];
		}

		public int GetLength(int dimension) {
			return array.GetLength(dimension);
		}

		public Complex this[int i, int j] {
			get {
				return array[i,j];
			}
			set {
				array[i,j] = value;
			}
		}

		public IComplexArray GetRow(int j)
		{
			return new ComplexArray1D(array,j,false);
		}

		public IComplexArray GetCol(int i)
		{
			return new ComplexArray1D(array,i,true);
		}

		public class ComplexArray1D : IComplexArray
		{
			Complex[,] array;
			int index;
			bool isColumn;

			public ComplexArray1D(Complex[,] arr, int index, bool isCol)
			{
				this.array = arr;
				this.index = index;
				this.isColumn = isCol;
			}

			public int Length { get {
				return array.GetLength(isColumn ? 1 : 0);
			}}

			public Complex this[int i] {
				get {
					return isColumn ? array[index,i] : array[i,index];
				}
				set {
					if (isColumn) {
						array[index,i] = value;
					} else {
						array[i,index] = value;
					}
				}
			}
		}
	}
}
