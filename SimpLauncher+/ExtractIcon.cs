using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SimpLauncherPlus {
	public class ExtractIcon {
		[StructLayout(LayoutKind.Sequential)]
		struct SIZE {
			public int cx;
			public int cy;
			public SIZE(int cx, int cy) {
				this.cx = cx;
				this.cy = cy;
			}
		}
		[Flags]
		enum SIGDN : uint {
			NORMALDISPLAY = 0,
			PARENTRELATIVEPARSING = 0x80018001,
			PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
			DESKTOPABSOLUTEPARSING = 0x80028000,
			PARENTRELATIVEEDITING = 0x80031001,
			DESKTOPABSOLUTEEDITING = 0x8004c000,
			FILESYSPATH = 0x80058000,
			URL = 0x80068000
		}
		[Flags]
		enum SIIGBF {
			SIIGBF_RESIZETOFIT = 0,
			SIIGBF_BIGGERSIZEOK = 0x1,
			SIIGBF_MEMORYONLY = 0x2,
			SIIGBF_ICONONLY = 0x4,
			SIIGBF_THUMBNAILONLY = 0x8,
			SIIGBF_INCACHEONLY = 0x10
		};
		[ComImport]
		[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IShellItem {
			void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)]Guid bhid, [MarshalAs(UnmanagedType.LPStruct)]Guid riid, out IntPtr ppv);
			void GetParent(out IShellItem ppsi);
			void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
			void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
			void Compare(IShellItem psi, uint hint, out int piOrder);
		}
		[ComImportAttribute()]
		[GuidAttribute("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		interface IShellItemImageFactory {
			void GetImage([In, MarshalAs(UnmanagedType.Struct)] SIZE size, [In] SIIGBF flags, [Out] out IntPtr phbm);
		}
		[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		static extern void SHCreateItemFromParsingName([In][MarshalAs(UnmanagedType.LPWStr)] string pszPath, [In] IntPtr pbc, [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);
		[DllImport("gdi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject(IntPtr hObject);
		static IntPtr GetFileIconImage(string path) {
			IShellItem shell = null;
			IntPtr hbitmap = IntPtr.Zero;
			SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItem).GUID, out shell);
			
			((IShellItemImageFactory)shell).GetImage(new SIZE(70, 70), SIIGBF.SIIGBF_ICONONLY, out hbitmap);
			Marshal.ReleaseComObject(shell);
			return hbitmap;
		}
		static BitmapSource ConverterBitmapImage(IntPtr bmp) {
			BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			DeleteObject(bmp);
			return bs;
		}
		static BitmapSource ConverterBitmapImage(IntPtr bmp, System.Drawing.Size size) {
			BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
			DeleteObject(bmp);
			return bs;
		}
		public static BitmapSource Get(string path) {
			return ConverterBitmapImage(GetFileIconImage(path));
		}
		public static BitmapSource Get(string path, System.Drawing.Size size) {
			return ConverterBitmapImage(GetFileIconImage(path), size);
		}
	}
}
