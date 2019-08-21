using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Bee.Logging;

namespace Bee.Util
{
    /// <summary>
    /// 图片
    /// </summary>
    public enum ThumbNailScale
    {
        /// <summary>
        /// 指定宽，高按比例    
        /// </summary>
        ScaleWidth,
        /// <summary>
        /// 指定高，宽按比例
        /// </summary>
        ScaleHeight,
        /// <summary>
        /// 指定高宽裁减，可能只显示部分图片
        /// </summary>
        Cut,
        /// <summary>
        /// 按图片比例缩放，不变形，显示全部图片（推荐）
        /// </summary>
        ScaleDown
    }

    public enum ImagePosition
    {
        /// <summary>
        /// 居中
        /// </summary>
        Center,
        /// <summary>
        /// 左上角
        /// </summary>
        TopLeft,
        /// <summary>
        /// 左下角
        /// </summary>
        BottomLeft,
        /// <summary>
        /// 右下角
        /// </summary>
        BottomRight,
        /// <summary>
        /// 右上角
        /// </summary>
        TopRigth
    }

    public static class DrawingUtil
    {
        /// <summary>
        /// Create the image by the valid code.
        /// </summary>
        /// <param name="validCode">the valid code.</param>
        /// <returns>the image of the valid code.</returns>
        public static Bitmap CreateImage(string validCode)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(validCode, "validCode");

            int iwidth = (int)(validCode.Length * 15);
            System.Drawing.Bitmap image = new System.Drawing.Bitmap(iwidth, 25);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.White);

                Color[] c = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.SteelBlue, Color.Black, Color.Purple };
                string[] font = { "Verdana", "Microsoft Sans Serif", "Comic Sans MS", "Arial", "宋体" };
                Random rand = new Random();
                for (int i = 0; i < 25; i++)
                {
                    int x1 = rand.Next(image.Width);
                    int x2 = rand.Next(image.Width);
                    int y1 = rand.Next(image.Height);
                    int y2 = rand.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }
                for (int i = 0; i < validCode.Length; i++)
                {
                    int cindex = rand.Next(7);
                    int findex = rand.Next(5);

                    Font f = new System.Drawing.Font(font[findex], 14, System.Drawing.FontStyle.Bold);
                    Brush b = new System.Drawing.SolidBrush(c[cindex]);
                    int ii = 2;
                    if ((i + 1) % 2 == 0)
                    {
                        ii = 0;
                    }
                    g.DrawString(validCode.Substring(i, 1), f, b, (i * 13), ii);
                }

                for (int i = 0; i < 100; i++)
                {
                    int x = rand.Next(image.Width);
                    int y = rand.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(rand.Next()));
                }

                g.DrawRectangle(new Pen(Color.Black, 0), 0, 0, image.Width - 1, image.Height - 1);

            }

            return image;
        }


        public static bool GetThumbNail(Image img, string savePath, int width, int height, ThumbNailScale scale)
        {
            //缩略图
            int towidth = width;
            int toheight = height;
            int x = 0;
            int y = 0;
            int ow = img.Width;
            int oh = img.Height;
            //如果图片小于指定宽度
            if (ow < width)
                width = ow;

            if (oh < height)
                height = oh;


            switch (scale)
            {
                case ThumbNailScale.ScaleWidth:
                    toheight = img.Height * width / img.Width;
                    break;
                case ThumbNailScale.ScaleHeight:
                    towidth = img.Width * height / img.Height;
                    break;
                case ThumbNailScale.Cut:
                    if ((double)img.Width / (double)img.Height > (double)towidth / (double)toheight)
                    {
                        oh = img.Height;
                        ow = img.Height * towidth / toheight;
                        y = 0;
                        x = (img.Width - ow) / 2;
                    }
                    else
                    {
                        ow = img.Width;
                        oh = img.Width * height / towidth;
                        x = 0;
                        y = (img.Height - oh) / 2;
                    }
                    break;
                case ThumbNailScale.ScaleDown:
                    double Tw, Th;
                    Tw = width;
                    Th = height * (Convert.ToDouble(oh) / Convert.ToDouble(ow));
                    if (Th > height)
                    {
                        Th = height;
                        Tw = width * (Convert.ToDouble(ow) / Convert.ToDouble(oh));
                    }
                    towidth = Convert.ToInt32(Tw);
                    toheight = Convert.ToInt32(Th);
                    break;
                default:
                    break;
            }

            //新建一个bmp图片
            Image bitmap = new Bitmap(towidth, toheight);

            //新建一个画板
            Graphics g = Graphics.FromImage(bitmap);

            //设置高质量插值法
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充
            g.Clear(Color.Transparent);


            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(img, new Rectangle(0, 0, towidth, toheight),
                new Rectangle(x, y, ow, oh),
                GraphicsUnit.Pixel);

            try
            {
                //以jpg格式保存缩略图
                bitmap.Save(savePath, ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("从文件流生成缩略图：{0}", ex.Message));
                return false;
            }
            finally
            {
                img.Dispose();
                bitmap.Dispose();
                g.Dispose();
            }

            return true;
        }

        public static void CaptureImage(Image img, string saveFilePath, int width, int height, int spaceX, int spaceY, int destWidth)
        {
            int x = 0; //截取X坐标
            int y = 0; //截取Y坐标
            //原图宽与生成图片宽 之差
            //当小于0(即原图宽小于要生成的图)时，新图宽度为较小者 即原图宽度 X坐标则为0
            //当大于0(即原图宽大于要生成的图)时，新图宽度为设置值 即width X坐标则为 sX与spaceX之间较小者
            //Y方向同理
            int sX = img.Width - width;
            int sY = img.Height - height;
            if (sX > 0)
            {
                x = sX > spaceX ? spaceX : sX;
            }
            else
            {
                width = img.Width;
            }
            if (sY > 0)
            {
                y = sY > spaceY ? spaceY : sY;
            }
            else
            {
                height = img.Height;
            }

            //创建新图位图
            if(destWidth <= 0)
            {
                destWidth = width;
            }

            int destHeight = destWidth * height / width;
            Bitmap bitmap = new Bitmap(destWidth, destHeight);
            //创建作图区域
            Graphics graphic = Graphics.FromImage(bitmap);
            //截取原图相应区域写入作图区
            graphic.DrawImage(img, new Rectangle(0, 0, destWidth, destHeight), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            //从作图区生成新图
            Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());
            //保存图象
            saveImage.Save(saveFilePath, ImageFormat.Jpeg);
            //释放资源
            saveImage.Dispose();
            bitmap.Dispose();
            graphic.Dispose();
        }

        public static bool MakeWaterImage(Stream sourceFile, string saveFile, ImagePosition Position)
        {
            bool result = false;
            //水印图片
            try
            {
                Image imgPhoto = Image.FromStream(sourceFile);


                int phWidth = imgPhoto.Width;
                int phHeight = imgPhoto.Height;

                Bitmap bmPhoto = new Bitmap(phWidth, phHeight, PixelFormat.Format24bppRgb);
                bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

                Image imgWatermark = new Bitmap(System.Web.HttpContext.Current.Server.MapPath("/images/watermark.png"));
                int wmWidth = imgWatermark.Width;
                int wmHeight = imgWatermark.Height;

                if (phWidth > (wmWidth + 100) && phHeight > (wmHeight + 100))
                {
                    Graphics grPhoto = Graphics.FromImage(bmPhoto);
                    grPhoto.Clear(Color.White);
                    grPhoto.DrawImage(imgPhoto, new Rectangle(0, 0, phWidth, phHeight), 0, 0, phWidth, phHeight, GraphicsUnit.Pixel);

                    grPhoto.Dispose();

                    //添加水印图片

                    using (Bitmap bmWatermark = new Bitmap(bmPhoto))
                    {
                        bmWatermark.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);
                        Graphics grWatermark = Graphics.FromImage(bmWatermark);
                        using (ImageAttributes imageAttributes = new ImageAttributes())
                        {
                            //ColorMap colorMap = new ColorMap();
                            //colorMap.OldColor = Color.FromArgb(255, 255, 255,255);
                            //colorMap.NewColor = Color.FromArgb(0, 0, 0, 0);
                            //ColorMap[] remapTable = { colorMap };
                            //imageAttributes.SetRemapTable(remapTable, ColorAdjustType.Bitmap);
                            float[][] colorMatrixElements = { new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f }, new float[] { 0.0f, 1.0f, 0.0f, 0.0f, 0.0f }, new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 0.0f }, new float[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f }, new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f } };
                            ColorMatrix wmColorMatrix = new ColorMatrix(colorMatrixElements);
                            imageAttributes.SetColorMatrix(wmColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            int xPosOfWm = 0;
                            int yPosOfWm = 0;
                            switch (Position)
                            {
                                case ImagePosition.BottomRight:
                                    xPosOfWm = ((phWidth - wmWidth) - 2);
                                    yPosOfWm = ((phHeight - wmHeight) - 2);
                                    break;
                                case ImagePosition.TopLeft:
                                    xPosOfWm = 2;
                                    yPosOfWm = 2;
                                    break;
                                case ImagePosition.TopRigth:
                                    xPosOfWm = ((phWidth - wmWidth) - 2);
                                    yPosOfWm = 2;
                                    break;
                                case ImagePosition.BottomLeft:
                                    xPosOfWm = 2;
                                    yPosOfWm = ((phHeight - wmHeight) - 2);
                                    break;
                                case ImagePosition.Center:
                                    xPosOfWm = ((phWidth / 2) - (wmWidth / 2));
                                    yPosOfWm = ((phHeight / 2) - (wmHeight / 2));
                                    break;
                            }
                            grWatermark.DrawImage(imgWatermark, new Rectangle(xPosOfWm, yPosOfWm, wmWidth, wmHeight), 0, 0, wmWidth, wmHeight, GraphicsUnit.Pixel, imageAttributes);
                        }
                        imgPhoto = bmWatermark;
                        grWatermark.Dispose();
                        imgPhoto.Save(saveFile, ImageFormat.Jpeg);
                    }

                    result = true;
                }
                else
                {
                    Image imgPhoto2 = Image.FromStream(sourceFile);
                    imgPhoto2.Save(saveFile, ImageFormat.Jpeg);
                    imgPhoto2.Dispose();
                    result = true;
                }
                imgWatermark.Dispose();
                bmPhoto.Dispose();
                imgPhoto.Dispose();
            }
            catch
            {

                try
                {
                    Image imgPhoto2 = Image.FromStream(sourceFile);
                    imgPhoto2.Save(saveFile, ImageFormat.Jpeg);
                    imgPhoto2.Dispose();
                    result = true;
                }
                catch
                {
                    result = false;
                }
            }

            sourceFile.Close();
            sourceFile.Dispose();

            return result;

        }

        /// <summary>
        /// Calc the hash value of the image.
        /// according to :http://www.hackerfactor.com/blog/index.php?/archives/432-Looks-Like-It.html
        /// </summary>
        /// <param name="src">the image</param>
        /// <returns>the hash value.</returns>
        public static string GetImageHash(Image src)
        {
            // Reduce size to 8*8
            Image image = src.GetThumbnailImage(8, 8, () => { return false; }, IntPtr.Zero);
            // Reduce Color
            Bitmap bitMap = new Bitmap(image);
            Byte[] grayValues = new Byte[image.Width * image.Height];

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = bitMap.GetPixel(x, y);
                    byte grayValue = (byte)((color.R * 30 + color.G * 59 + color.B * 11) / 100);
                    grayValues[x * image.Width + y] = grayValue;
                }
            }

            int sum = 0;
            for (int i = 0; i < grayValues.Length; i++)
            {
                sum += (int)grayValues[i];
            }
            Byte average = Convert.ToByte(sum / grayValues.Length);

            char[] result = new char[grayValues.Length];
            for (int i = 0; i < grayValues.Length; i++)
            {
                if (grayValues[i] < average)
                    result[i] = '0';
                else
                    result[i] = '1';
            }
            return new String(result);
        }

        public static Int32 CompareImageHash(string a, string b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException();
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    count++;
            }
            return count;
        }

        public static BeeDataAdapter GetExifInfo(Image src)
        {
            BeeDataAdapter result = new BeeDataAdapter();
            Bee.Util.EXIFMetaData.Metadata metadata = EXIFMetaData.GetEXIFMetaData(src);

            string model = metadata.CameraModel.DisplayValue;
            if (model != null && model.EndsWith("\0"))
            {
                model = model.Substring(0, model.Length - 1);
            }
            result.Add("Camera", model);
            result.Add("Aperture", metadata.Aperture.DisplayValue);

            result.Add("ShutterSpeed", metadata.ShutterSpeed.DisplayValue);
            result.Add("Flash", metadata.Flash.DisplayValue);
            result.Add("ImageWidth", metadata.ImageWidth.DisplayValue);
            result.Add("ImageHeight", metadata.ImageHeight.DisplayValue);

            result.Add("FNum", metadata.FNumber.DisplayValue);
            result.Add("ISOSpeed", metadata.ISOSpeed.DisplayValue);
            result.Add("ExposureTime", metadata.ExposureTime.DisplayValue);

            DateTime pictureTime = DateTime.Now;
            if (metadata.CreateTime.DisplayValue != null)
            {
                if (DateTime.TryParse(metadata.CreateTime.DisplayValue, out pictureTime))
                {
                    result.Add("PictureTime", pictureTime);
                }
                else
                {
                    string[] items = metadata.CreateTime.DisplayValue.Split(':', ' ');
                    if (items.Length == 6)
                    {
                        List<int> list = new List<int>();
                        int intValue = 0;
                        foreach (string item in items)
                        {
                            if (int.TryParse(item, out intValue))
                            {
                                list.Add(intValue);
                            }
                        }
                        if (list.Count == 6)
                        {
                            pictureTime = new DateTime(list[0], list[1], list[2], list[3], list[4], list[5]);
                            result.Add("PictureTime", pictureTime);
                        }
                    }
                }
            }

            
            result.Add("Latitude", metadata.Latitude.DisplayValue);
            result.Add("Longitude", metadata.Longitude.DisplayValue);

            return result;
        }
    }

    internal class EXIFMetaData
    {
        #region 数据转换结构
        /// <summary> 
        /// 转换数据结构 
        /// </summary> 
        public struct MetadataDetail
        {
            public string Hex;//十六进制字符串 
            public string RawValueAsString;//原始值串 
            public string DisplayValue;//显示值串 
        }
        #endregion

        #region EXIF元素结构
        /// <summary> 
        /// 结构：存储EXIF元素信息 
        /// </summary> 
        public struct Metadata
        {
            public MetadataDetail EquipmentMake;
            public MetadataDetail CameraModel;
            public MetadataDetail ExposureTime;//曝光时间 
            public MetadataDetail Fstop;
            public MetadataDetail DatePictureTaken;
            public MetadataDetail ShutterSpeed;// 快门速度 
            public MetadataDetail MeteringMode;//曝光模式 
            public MetadataDetail Flash;//闪光灯 
            public MetadataDetail XResolution;
            public MetadataDetail YResolution;
            public MetadataDetail ImageWidth;//照片宽度 
            public MetadataDetail ImageHeight;//照片高度 

            public MetadataDetail FNumber;// added f值，光圈数 
            public MetadataDetail ExposureProg;// added 曝光程序 
            public MetadataDetail SpectralSense;// added  
            public MetadataDetail ISOSpeed;// added ISO感光度 
            public MetadataDetail OECF;// added  
            public MetadataDetail Ver;// added EXIF版本 
            public MetadataDetail CompConfig;// added 色彩设置 
            public MetadataDetail CompBPP;// added 压缩比率 
            public MetadataDetail Aperture;// added 光圈值 
            public MetadataDetail Brightness;// added 亮度值Ev 
            public MetadataDetail ExposureBias;// added 曝光补偿 
            public MetadataDetail MaxAperture;// added 最大光圈值 

            public MetadataDetail SubjectDist;// added主体距离 
            public MetadataDetail LightSource;// added 白平衡 
            public MetadataDetail FocalLength;// added 焦距 
            public MetadataDetail FPXVer;// added FlashPix版本 
            public MetadataDetail ColorSpace;// added 色彩空间 
            public MetadataDetail Interop;// added  
            public MetadataDetail FlashEnergy;// added  
            public MetadataDetail SpatialFR;// added  
            public MetadataDetail FocalXRes;// added  
            public MetadataDetail FocalYRes;// added  
            public MetadataDetail FocalResUnit;// added  
            public MetadataDetail ExposureIndex;// added 曝光指数 
            public MetadataDetail SensingMethod;// added 感应方式 
            public MetadataDetail SceneType;// added  
            public MetadataDetail CfaPattern;// added  
            public MetadataDetail CreateTime; // this.Add(0x132,"Date Time");
            public MetadataDetail Latitude; //this.Add(0x2,"Gps Latitude");
            public MetadataDetail Longitude; //this.Add(0x4,"Gps Latitude");
        }
        #endregion

        #region 查找EXIF元素值
        public static string LookupEXIFValue(string Description, string Value)
        {
            string DescriptionValue = null;

            switch (Description)
            {
                case "MeteringMode":

                    #region  MeteringMode
                    {
                        switch (Value)
                        {
                            case "0":
                                DescriptionValue = "Unknown"; break;
                            case "1":
                                DescriptionValue = "Average"; break;
                            case "2":
                                DescriptionValue = "Center Weighted Average"; break;
                            case "3":
                                DescriptionValue = "Spot"; break;
                            case "4":
                                DescriptionValue = "Multi-spot"; break;
                            case "5":
                                DescriptionValue = "Multi-segment"; break;
                            case "6":
                                DescriptionValue = "Partial"; break;
                            case "255":
                                DescriptionValue = "Other"; break;
                        }
                    }
                    #endregion

                    break;
                case "ResolutionUnit":

                    #region ResolutionUnit
                    {
                        switch (Value)
                        {
                            case "1":
                                DescriptionValue = "No Units"; break;
                            case "2":
                                DescriptionValue = "Inch"; break;
                            case "3":
                                DescriptionValue = "Centimeter"; break;
                        }
                    }

                    #endregion

                    break;
                case "Flash":

                    #region Flash
                    {
                        switch (Value)
                        {
                            case "0":
                                DescriptionValue = "未使用"; break;
                            case "1":
                                DescriptionValue = "闪光"; break;
                            case "5":
                                DescriptionValue = "Flash fired but strobe return light not detected"; break;
                            case "7":
                                DescriptionValue = "Flash fired and strobe return light detected"; break;
                        }
                    }
                    #endregion

                    break;
                case "ExposureProg":

                    #region ExposureProg
                    {
                        switch (Value)
                        {
                            case "0":
                                DescriptionValue = "没有定义"; break;
                            case "1":
                                DescriptionValue = "手动控制"; break;
                            case "2":
                                DescriptionValue = "程序控制"; break;
                            case "3":
                                DescriptionValue = "光圈优先"; break;
                            case "4":
                                DescriptionValue = "快门优先"; break;
                            case "5":
                                DescriptionValue = "夜景模式"; break;
                            case "6":
                                DescriptionValue = "运动模式"; break;
                            case "7":
                                DescriptionValue = "肖像模式"; break;
                            case "8":
                                DescriptionValue = "风景模式"; break;
                            case "9":
                                DescriptionValue = "保留的"; break;
                        }
                    }

                    #endregion

                    break;
                case "CompConfig":

                    #region CompConfig
                    {
                        switch (Value)
                        {

                            case "513":
                                DescriptionValue = "YCbCr"; break;
                        }
                    }
                    #endregion

                    break;
                case "Aperture":

                    #region Aperture
                    DescriptionValue = Value;
                    #endregion

                    break;
                case "LightSource":

                    #region LightSource
                    {
                        switch (Value)
                        {
                            case "0":
                                DescriptionValue = "未知"; break;
                            case "1":
                                DescriptionValue = "日光"; break;
                            case "2":
                                DescriptionValue = "荧光灯"; break;
                            case "3":
                                DescriptionValue = "白炽灯"; break;
                            case "10":
                                DescriptionValue = "闪光灯"; break;
                            case "17":
                                DescriptionValue = "标准光A"; break;
                            case "18":
                                DescriptionValue = "标准光B"; break;
                            case "19":
                                DescriptionValue = "标准光C"; break;
                            case "20":
                                DescriptionValue = "标准光D55"; break;
                            case "21":
                                DescriptionValue = "标准光D65"; break;
                            case "22":
                                DescriptionValue = "标准光D75"; break;
                            case "255":
                                DescriptionValue = "其它"; break;
                        }
                    }


                    #endregion
                    break;

            }
            return DescriptionValue;
        }
        #endregion

        #region 取得图片的EXIF信息
        public static Metadata GetEXIFMetaData(Image MyImage)
        {
            // 创建一个整型数组来存储图像中属性数组的ID 
            int[] MyPropertyIdList = MyImage.PropertyIdList;
            //创建一个封闭图像属性数组的实例 
            PropertyItem[] MyPropertyItemList = new PropertyItem[MyPropertyIdList.Length];
            //创建一个图像EXIT信息的实例结构对象，并且赋初值 

            #region 创建一个图像EXIT信息的实例结构对象，并且赋初值
            Metadata MyMetadata = new Metadata();
            MyMetadata.EquipmentMake.Hex = "10f";
            MyMetadata.CameraModel.Hex = "110";
            MyMetadata.DatePictureTaken.Hex = "9003";
            MyMetadata.ExposureTime.Hex = "829a";
            MyMetadata.Fstop.Hex = "829d";
            MyMetadata.ShutterSpeed.Hex = "9201";
            MyMetadata.MeteringMode.Hex = "9207";
            MyMetadata.Flash.Hex = "9209";
            MyMetadata.FNumber.Hex = "829d"; //added  
            MyMetadata.ExposureProg.Hex = ""; //added  
            MyMetadata.SpectralSense.Hex = "8824"; //added  
            MyMetadata.ISOSpeed.Hex = "8827"; //added  
            MyMetadata.OECF.Hex = "8828"; //added  
            MyMetadata.Ver.Hex = "9000"; //added  
            MyMetadata.CompConfig.Hex = "9101"; //added  
            MyMetadata.CompBPP.Hex = "9102"; //added  
            MyMetadata.Aperture.Hex = "9202"; //added  
            MyMetadata.Brightness.Hex = "9203"; //added  
            MyMetadata.ExposureBias.Hex = "9204"; //added  
            MyMetadata.MaxAperture.Hex = "9205"; //added  
            MyMetadata.SubjectDist.Hex = "9206"; //added  
            MyMetadata.LightSource.Hex = "9208"; //added  
            MyMetadata.FocalLength.Hex = "920a"; //added  
            MyMetadata.FPXVer.Hex = "a000"; //added  
            MyMetadata.ColorSpace.Hex = "a001"; //added  
            MyMetadata.FocalXRes.Hex = "a20e"; //added  
            MyMetadata.FocalYRes.Hex = "a20f"; //added  
            MyMetadata.FocalResUnit.Hex = "a210"; //added  
            MyMetadata.ExposureIndex.Hex = "a215"; //added  
            MyMetadata.SensingMethod.Hex = "a217"; //added  
            MyMetadata.SceneType.Hex = "a301";
            MyMetadata.CfaPattern.Hex = "a302";
            #endregion

            // ASCII编码 
            System.Text.ASCIIEncoding Value = new System.Text.ASCIIEncoding();

            int index = 0;
            int MyPropertyIdListCount = MyPropertyIdList.Length;
            if (MyPropertyIdListCount != 0)
            {
                foreach (int MyPropertyId in MyPropertyIdList)
                {
                    string hexVal = "";
                    MyPropertyItemList[index] = MyImage.GetPropertyItem(MyPropertyId);

                    #region 初始化各属性值
                    string myPropertyIdString = MyImage.GetPropertyItem(MyPropertyId).Id.ToString("x");
                    switch (myPropertyIdString)
                    {
                        case "2":
                            {
                                MyMetadata.Latitude.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.Latitude.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "4":
                            {
                                MyMetadata.Longitude.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.Longitude.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "132":
                            {
                                MyMetadata.CreateTime.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.CreateTime.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "10f":
                            {
                                MyMetadata.EquipmentMake.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.EquipmentMake.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "110":
                            {
                                MyMetadata.CameraModel.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.CameraModel.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;

                            }

                        case "9003":
                            {
                                MyMetadata.DatePictureTaken.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.DatePictureTaken.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "9207":
                            {
                                MyMetadata.MeteringMode.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.MeteringMode.DisplayValue = LookupEXIFValue("MeteringMode", BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString());
                                break;
                            }

                        case "9209":
                            {
                                MyMetadata.Flash.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.Flash.DisplayValue = LookupEXIFValue("Flash", BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString());
                                break;
                            }

                        case "829a":
                            {
                                MyMetadata.ExposureTime.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                string StringValue = "";
                                for (int Offset = 0; Offset < MyImage.GetPropertyItem(MyPropertyId).Len; Offset = Offset + 4)
                                {
                                    StringValue += BitConverter.ToInt32(MyImage.GetPropertyItem(MyPropertyId).Value, Offset).ToString() + "/";
                                }
                                MyMetadata.ExposureTime.DisplayValue = StringValue.Substring(0, StringValue.Length - 1);
                                break;
                            }
                        case "829d":
                            {
                                MyMetadata.Fstop.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                int int1;
                                int int2;
                                int1 = BitConverter.ToInt32(MyImage.GetPropertyItem(MyPropertyId).Value, 0);
                                int2 = BitConverter.ToInt32(MyImage.GetPropertyItem(MyPropertyId).Value, 4);
                                MyMetadata.Fstop.DisplayValue = "F/" + (int1 / int2);

                                MyMetadata.FNumber.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.FNumber.DisplayValue = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();

                                break;
                            }
                        case "9201":
                            {
                                MyMetadata.ShutterSpeed.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                string StringValue = BitConverter.ToInt32(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                MyMetadata.ShutterSpeed.DisplayValue = "1/" + StringValue;
                                break;
                            }

                        case "8822":
                            {
                                MyMetadata.ExposureProg.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.ExposureProg.DisplayValue = LookupEXIFValue("ExposureProg", BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString());
                                break;
                            }

                        case "8824":
                            {
                                MyMetadata.SpectralSense.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.SpectralSense.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }
                        case "8827":
                            {
                                hexVal = "";
                                MyMetadata.ISOSpeed.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                hexVal = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value).Substring(0, 2);
                                MyMetadata.ISOSpeed.DisplayValue = Convert.ToInt32(hexVal, 16).ToString();//Value.GetString(MyPropertyItemList[index].Value); 
                                break;
                            }

                        case "8828":
                            {
                                MyMetadata.OECF.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.OECF.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "9000":
                            {
                                MyMetadata.Ver.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.Ver.DisplayValue = Value.GetString(MyPropertyItemList[index].Value).Substring(1, 1) + "." + Value.GetString(MyPropertyItemList[index].Value).Substring(2, 2);
                                break;
                            }

                        case "9101":
                            {
                                MyMetadata.CompConfig.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.CompConfig.DisplayValue = LookupEXIFValue("CompConfig", BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString());
                                break;
                            }

                        case "9102":
                            {
                                MyMetadata.CompBPP.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.CompBPP.DisplayValue = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                break;
                            }

                        case "9202":
                            {
                                hexVal = "";
                                MyMetadata.Aperture.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                hexVal = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value).Substring(0, 2);
                                hexVal = Convert.ToInt32(hexVal, 16).ToString();
                                hexVal = hexVal + "00";
                                MyMetadata.Aperture.DisplayValue = hexVal.Substring(0, 1) + "." + hexVal.Substring(1, 2);
                                break;
                            }

                        case "9203":
                            {
                                hexVal = "";
                                MyMetadata.Brightness.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                hexVal = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value).Substring(0, 2);
                                hexVal = Convert.ToInt32(hexVal, 16).ToString();
                                hexVal = hexVal + "00";
                                MyMetadata.Brightness.DisplayValue = hexVal.Substring(0, 1) + "." + hexVal.Substring(1, 2);
                                break;
                            }

                        case "9204":
                            {
                                MyMetadata.ExposureBias.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.ExposureBias.DisplayValue = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                break;
                            }

                        case "9205":
                            {
                                hexVal = "";
                                MyMetadata.MaxAperture.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                hexVal = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value).Substring(0, 2);
                                hexVal = Convert.ToInt32(hexVal, 16).ToString();
                                hexVal = hexVal + "00";
                                MyMetadata.MaxAperture.DisplayValue = hexVal.Substring(0, 1) + "." + hexVal.Substring(1, 2);
                                break;
                            }

                        case "9206":
                            {
                                MyMetadata.SubjectDist.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.SubjectDist.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "9208":
                            {
                                MyMetadata.LightSource.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.LightSource.DisplayValue = LookupEXIFValue("LightSource", BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString());
                                break;
                            }

                        case "920a":
                            {
                                hexVal = "";
                                MyMetadata.FocalLength.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                hexVal = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value).Substring(0, 2);
                                hexVal = Convert.ToInt32(hexVal, 16).ToString();
                                hexVal = hexVal + "00";
                                MyMetadata.FocalLength.DisplayValue = hexVal.Substring(0, 1) + "." + hexVal.Substring(1, 2);
                                break;
                            }

                        case "a000":
                            {
                                MyMetadata.FPXVer.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.FPXVer.DisplayValue = Value.GetString(MyPropertyItemList[index].Value).Substring(1, 1) + "." + Value.GetString(MyPropertyItemList[index].Value).Substring(2, 2);
                                break;
                            }

                        case "a001":
                            {
                                MyMetadata.ColorSpace.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                if (BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString() == "1")
                                    MyMetadata.ColorSpace.DisplayValue = "RGB";
                                if (BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString() == "65535")
                                    MyMetadata.ColorSpace.DisplayValue = "Uncalibrated";
                                break;
                            }

                        case "a20e":
                            {
                                MyMetadata.FocalXRes.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.FocalXRes.DisplayValue = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                break;
                            }

                        case "a20f":
                            {
                                MyMetadata.FocalYRes.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.FocalYRes.DisplayValue = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                break;
                            }

                        case "a210":
                            {
                                string aa;
                                MyMetadata.FocalResUnit.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                aa = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString(); ;
                                if (aa == "1") MyMetadata.FocalResUnit.DisplayValue = "没有单位";
                                if (aa == "2") MyMetadata.FocalResUnit.DisplayValue = "英尺";
                                if (aa == "3") MyMetadata.FocalResUnit.DisplayValue = "厘米";
                                break;
                            }

                        case "a215":
                            {
                                MyMetadata.ExposureIndex.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.ExposureIndex.DisplayValue = Value.GetString(MyPropertyItemList[index].Value);
                                break;
                            }

                        case "a217":
                            {
                                string aa;
                                aa = BitConverter.ToInt16(MyImage.GetPropertyItem(MyPropertyId).Value, 0).ToString();
                                MyMetadata.SensingMethod.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                if (aa == "2") MyMetadata.SensingMethod.DisplayValue = "1 chip color area sensor";
                                break;
                            }

                        case "a301":
                            {
                                MyMetadata.SceneType.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.SceneType.DisplayValue = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                break;
                            }

                        case "a302":
                            {
                                MyMetadata.CfaPattern.RawValueAsString = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                MyMetadata.CfaPattern.DisplayValue = BitConverter.ToString(MyImage.GetPropertyItem(MyPropertyId).Value);
                                break;
                            }



                    }
                    #endregion

                    index++;
                }
            }

            MyMetadata.XResolution.DisplayValue = MyImage.HorizontalResolution.ToString();
            MyMetadata.YResolution.DisplayValue = MyImage.VerticalResolution.ToString();
            MyMetadata.ImageHeight.DisplayValue = MyImage.Height.ToString();
            MyMetadata.ImageWidth.DisplayValue = MyImage.Width.ToString();

            return MyMetadata;
        }
        #endregion
    }
}
