# RasDenoise
Image Denoising utility

## Usage
```
Usage: RasDenoise (method) [options] [file1 file2 ...]
 Methods:
  [0] Help                  Show full help
  [1] NlMeans               Denoising using Non-local Means algorithm
  [2] NlMeansColored        Denoising using Non-local Means algorithm (modified for color)
  [3] Dct                   Simple dct-based denoising
  [4] TVL1                  Denoising via primal-dual algorithm
  [5] DFTForward            Transform image using forward fourier transform
  [6] DFTInverse            Transform image(s) using inverse fourier transform

 Additional Information:

  [1] NlMeans
  Perform image denoising using Non-local Means Denoising algorithm: http://www.ipol.im/pub/algo/bcm_non_local_means_de
  noising/ with several computational optimizations. Noise expected to be a gaussian white noise.

  (input image)         Input 8-bit 1-channel, 2-channel or 3-channel image.
  [output image]        Output image
  -h [float = 3.0]      Parameter regulating filter strength. Big h value perfectly removes noise but also removes imag
                        e details, smaller h value preserves details but also preserves some noise.
  -t [int = 7]          Size in pixels of the template patch that is used to compute weights. Should be odd.
  -s [int = 21]         Size in pixels of the window that is used to compute weighted average for given pixel. Should b
                        e odd. Affect performance linearly: greater searchWindowsSize - greater denoising time.

  [2] NlMeansColored
  Perform image denoising using Non-local Means Denoising algorithm (modified for color image): http://www.ipol.im/pub/
  algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white n
  oise. The function converts image to CIELAB colorspace and then separately denoise L and AB components with given h p
  arameters using fastNlMeansDenoising function.

  (input image)         Input 8-bit 1-channel, 2-channel or 3-channel image.
  [output image]        Output image
  -h [float = 3.0]      Parameter regulating filter strength. Big h value perfectly removes noise but also removes imag
                        e details, smaller h value preserves details but also preserves some noise.
  -c [float = 3.0]      The same as -h but for color components. For most images value equals 10 will be enought to rem
                        ove colored noise and do not distort colors.
  -t [int = 7]          Size in pixels of the template patch that is used to compute weights. Should be odd.
  -s [int = 21]         Size in pixels of the window that is used to compute weighted average for given pixel. Should b
                        e odd. Affect performance linearly: greater searchWindowsSize - greater denoising time.

  [3] Dct
  The function implements simple dct-based denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.

  (input image)         Source image
  [output image]        Output image
  -s (double)           Expected noise standard deviation
  -p [int = 16]         Size of block side where dct is computed

  [4] TVL1
  The function implements simple dct-based denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.

  (file) [file] [...]   One or more noised versions of the image that is to be restored.
  -o [output image]     Output image
  -l (double)           Corresponds to in the formulas above. As it is enlarged, the smooth (blurred) images are treate
                        d more favorably than detailed (but maybe more noised) ones. Roughly speaking, as it becomes sm
                        aller, the result will be more blur but more sever outliers will be removed.
  -n (int)              Number of iterations that the algorithm will run. Of course, as more iterations as better, but
                        it is hard to quantitatively refine this statement, so just use the default and increase it if
                        the results are poor.

  [5] DFTForward
  Decomposes an input image into frequency magnitude and phase components.

  (input image)         Source image
  -o [output image]     Output image (produces 2 files)

  [6] DFTInverse
  Recomposes an image from frequency magnitude and phase components.

  [output image]       Output image
  -m (input image)     Input magnitude image
  -p (input image)     Input phase image
  -mi (number)         magnitude range min
  -mx (number)         magnitude range max
  -pi (number)         phase range min
  -px (number)         phase range max
```