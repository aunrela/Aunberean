using AcClient;
using ACE.DatLoader.FileTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Common.Enums;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;
using Palette = ACE.DatLoader.FileTypes.Palette;

namespace Aunberean
{
    public unsafe static class IconHelpers
    {

        public static void ReplaceWhite(Bitmap bmp, Color newColor)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);

                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(pixel.A, newColor));
                    }
                }
            }
        }

        public static Bitmap ReplaceToColor(Bitmap bmp, Color newColor)
        {
            Bitmap resultBmp = (Bitmap)bmp.Clone();
            for (int y = 0; y < resultBmp.Height; y++)
            {
                for (int x = 0; x < resultBmp.Width; x++)
                {
                    Color pixel = resultBmp.GetPixel(x, y);

                    if (pixel.R > 254 && pixel.G > 254 && pixel.B > 254)
                    {
                        resultBmp.SetPixel(x, y, Color.FromArgb(pixel.A, newColor));
                    }
                }
            }
            return resultBmp;
        }
        static public Bitmap GetBitmap(uint iconID)
        {
            Bitmap defaultbmp = new(32, 32);
            if (iconID == 0) return defaultbmp;

            if (iconID < 100663296)
            {
                iconID += 100663296;
            }

            var tex = UBService.PortalDat.ReadFromDat<ACE.DatLoader.FileTypes.Texture>(iconID);
            if (tex != null)
            {
                Bitmap bmp = GetBitmap(tex);
                return bmp;
            }

            return defaultbmp;
        }
        public static Bitmap GetBitmap(ACE.DatLoader.FileTypes.Texture texture)
        {
            Bitmap bitmap = new Bitmap(texture.Width, texture.Height);
            List<int> imageColorArray = texture.GetImageColorArray();
            switch (texture.Format)
            {
                case SurfacePixelFormat.PFID_R8G8B8:
                case SurfacePixelFormat.PFID_CUSTOM_LSCAPE_R8G8B8:
                    {
                        for (int num7 = 0; num7 < texture.Height; num7++)
                        {
                            for (int num8 = 0; num8 < texture.Width; num8++)
                            {
                                int index4 = num7 * texture.Width + num8;
                                int red6 = (imageColorArray[index4] & 0xFF0000) >> 16;
                                int green6 = (imageColorArray[index4] & 0xFF00) >> 8;
                                int blue6 = imageColorArray[index4] & 0xFF;
                                bitmap.SetPixel(num8, num7, Color.FromArgb(red6, green6, blue6));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A8R8G8B8:
                    {
                        for (int num3 = 0; num3 < texture.Height; num3++)
                        {
                            for (int num4 = 0; num4 < texture.Width; num4++)
                            {
                                int index2 = num3 * texture.Width + num4;
                                int alpha3 = (int)((imageColorArray[index2] & 0xFF000000u) >> 24);
                                int red4 = (imageColorArray[index2] & 0xFF0000) >> 16;
                                int green4 = (imageColorArray[index2] & 0xFF00) >> 8;
                                int blue4 = imageColorArray[index2] & 0xFF;
                                bitmap.SetPixel(num4, num3, Color.FromArgb(alpha3, red4, green4, blue4));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_P8:
                case SurfacePixelFormat.PFID_INDEX16:
                    {
                        Palette palette = UBService.PortalDat.ReadFromDat<Palette>(texture.DefaultPaletteId.Value);
                        if (texture.CustomPaletteColors.Count > 0)
                        {
                            foreach (KeyValuePair<int, uint> customPaletteColor in texture.CustomPaletteColors)
                            {
                                if (customPaletteColor.Key <= palette.Colors.Count)
                                {
                                    palette.Colors[customPaletteColor.Key] = customPaletteColor.Value;
                                }
                            }
                        }

                        for (int k = 0; k < texture.Height; k++)
                        {
                            for (int l = 0; l < texture.Width; l++)
                            {
                                int index = k * texture.Width + l;
                                int alpha2 = (int)((palette.Colors[imageColorArray[index]] & 0xFF000000u) >> 24);
                                int red2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF0000) >> 16;
                                int green2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF00) >> 8;
                                int blue2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF);
                                bitmap.SetPixel(l, k, Color.FromArgb(alpha2, red2, green2, blue2));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A8:
                case SurfacePixelFormat.PFID_CUSTOM_LSCAPE_ALPHA:
                    {
                        for (int num5 = 0; num5 < texture.Height; num5++)
                        {
                            for (int num6 = 0; num6 < texture.Width; num6++)
                            {
                                int index3 = num5 * texture.Width + num6;
                                int red5 = imageColorArray[index3];
                                int green5 = imageColorArray[index3];
                                int blue5 = imageColorArray[index3];
                                bitmap.SetPixel(num6, num5, Color.FromArgb(red5, green5, blue5));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_R5G6B5:
                    {
                        for (int m = 0; m < texture.Height; m++)
                        {
                            for (int n = 0; n < texture.Width; n++)
                            {
                                int num2 = 3 * (m * texture.Width + n);
                                int red3 = imageColorArray[num2];
                                int green3 = imageColorArray[num2 + 1];
                                int blue3 = imageColorArray[num2 + 2];
                                bitmap.SetPixel(n, m, Color.FromArgb(red3, green3, blue3));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A4R4G4B4:
                    {
                        for (int i = 0; i < texture.Height; i++)
                        {
                            for (int j = 0; j < texture.Width; j++)
                            {
                                int num = 4 * (i * texture.Width + j);
                                int alpha = imageColorArray[num];
                                int red = imageColorArray[num + 1];
                                int green = imageColorArray[num + 2];
                                int blue = imageColorArray[num + 3];
                                bitmap.SetPixel(j, i, Color.FromArgb(alpha, red, green, blue));
                            }
                        }

                        break;
                    }
            }

            return bitmap;
        }

        public static int GetWeeniePtr(int character_id) { try { return Call_CObjectMaint__GetWeenieObject(character_id); } catch { return 0; } }

        public static int* CObjectMaint = (int*)0x00842ADC;
        public static int Call_CObjectMaint__GetWeenieObject(int object_id) => ((def_CObjectMaint__GetWeenieObject)Marshal.GetDelegateForFunctionPointer((IntPtr)0x005088E0, typeof(def_CObjectMaint__GetWeenieObject)))(*CObjectMaint, object_id);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)] internal delegate int def_CObjectMaint__GetWeenieObject(int CObjectMaint, int object_id); // HashBaseData<unsigned long> *__thiscall CObjectMaint::GetWeenieObject(CObjectMaint *this, unsigned int object_id)
        public unsafe static void InqDropIconInfo(IntPtr* _dropIcon, uint* _itemID, uint* _spellID, DropItemFlags* _flags)
        {
            ((delegate* unmanaged[Cdecl]<IntPtr*, uint*, uint*, DropItemFlags*, void>)5124992)(_dropIcon, _itemID, _spellID, _flags);
        }
        public static unsafe DragDropPayload GetDragDropInfo(UIElement* dragEl)
        {
            uint itemId = 0;
            uint spellId = 0;
            DropItemFlags flags;
            UIElement_ItemList.InqDropIconInfo(dragEl, &itemId, &spellId, &flags);

            return new DragDropPayload()
            {
                SpellId = spellId,
                ItemId = itemId,
                Flags = flags
            };
        }
    }
}
