using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.IO;
using OCR_Prototype.Models;
using System.Drawing;
using System.Drawing.Imaging;
using IronOcr;
using PdfToImage;
using System.Text.RegularExpressions;

namespace OCR_Prototype.Controllers
{
    public class OCRController : Controller
    {
        // GET: OCR
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Upload()
        {
            return View();
        }

        public ActionResult FormListing()
        {
            OCRModel ListResult = new OCRModel();

            return View(ListResult.getListing());
        }

        public ActionResult FormDetail(string id)
        {
            OCRModel getDetailResult = new OCRModel();

            return View(getDetailResult.getDetailList(id));
        }

        [HttpPost]
        public ActionResult UploadImg()
        {
            string path = "";
            string fileName = "";
            string convertpath = "";
            string filenamenoext = "";
            string fileext = "";
            List<int> ResultFormID = new List<int>();
            int DocID = 0;
            List<string> docpage = new List<string>();
            int totalpageno = 0;

            DocID = Convert.ToInt32(Request.Form["DDlDoc"].ToString());

            OCRModel imgpath = new OCRModel();

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    filenamenoext = Path.GetFileNameWithoutExtension(file.FileName);
                    fileName = Path.GetFileName(file.FileName);
                    fileext = Path.GetExtension(file.FileName);
                    path = Path.Combine(Server.MapPath("~/Images/"), fileName);

                    //Save File to destination folder
                    file.SaveAs(path);

                    if (fileext == ".pdf")
                    {
                        //Get pdf total page number
                        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                        StreamReader r = new StreamReader(fs);
                        string pdfText = r.ReadToEnd();
                        Regex rx1 = new Regex(@"/Type\s*/Page[^s]");
                        MatchCollection matches = rx1.Matches(pdfText);
                        totalpageno = matches.Count;

                        for (int i=1;i<= totalpageno;i++)
                        {
                            PdfToImage.PDFConvert pp = new PDFConvert();
                            pp.OutputFormat = "jpeg"; //format
                            pp.JPEGQuality = 100; //100% quality
                            pp.ResolutionX = 300; //dpi
                            pp.ResolutionY = 300;
                            pp.FirstPageToConvert = i; //pages you want
                            pp.LastPageToConvert = 2;
                            convertpath = Path.Combine(Server.MapPath("~/Images/"), "name_pg"+i+".jpeg");
                            pp.Convert(path, convertpath);

                            docpage.Add(convertpath);

                        }
                    }
                    else
                    {
                        //Non Pdf file 
                        docpage.Add(path);
                    }

                    //Save sourcefile information to DB
                    ResultFormID = imgpath.insertForm(docpage, DocID);                  
                }
            }

            if (ResultFormID.Count != 0)
            {
                OCRModel obj = new OCRModel();
                var PosResult = new List<OCRModel.Position>();

                //Get position for each box
                PosResult = imgpath.retrieveBoxPos(DocID);

                if (totalpageno == 0)
                {
                    int passID = ResultFormID[0];
                    cropImage_Convert(PosResult, path, filenamenoext, passID);
                }
                else
                {
                    //Crop Image base on position and convert to image
                    cropImage_ConvertMulti(PosResult, docpage, filenamenoext, ResultFormID, totalpageno);
                }
            }
            else
            {

            }
            return RedirectToAction("Upload");
        }

        public void cropImage_ConvertMulti(List<OCRModel.Position> CropPos, List<string> imagePath, string name, List<int> formID, int totalpage)
        {
            OCRModel imgpath = new OCRModel();
            OCRModel obj = new OCRModel();
            var CropRes = new List<OCRModel.CropResult>();
            int i = 0;
            string croppath = "";
            Bitmap croppedImage;

            for (int j=1; j<=totalpage;j++)
            {
                i = 0;

                for (i=0; i < CropPos.Count; i++)
                {
                    if (j == CropPos[i].page)
                    {
                        j--;
                        // Here we capture the resource - image file.
                        using (var originalImage = new Bitmap(imagePath[j]))
                        {
                            //Set Position {x1,y1,width,height}
                            Rectangle crop = new Rectangle(CropPos[i].pos_X1, CropPos[i].pos_Y1, CropPos[i].pos_width, CropPos[i].pos_height);

                            // Here we capture another resource.
                            croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);

                        }// Here we release the original resource - bitmap in memory and file on disk.

                        // At this point the file on disk already free - you can record to the same path.
                        //croppedImage.Save(@"C:\Users\kazarboys\Source\Repos\How-to-use-tesseract-ocr-4.0-with-csharp\tesseract-master.1153\samples\crop.jpg", ImageFormat.Jpeg);
                        croppath = Path.Combine(Server.MapPath("~/Images/Crop"), name + "" + formID[j] + "_crop_" + i + ".jpg");
                        croppedImage.Save(croppath, ImageFormat.Jpeg);

                        var Ocr = new AutoOcr();
                        var OcrResult = Ocr.Read(croppath);
                        Console.WriteLine(OcrResult.Text);

                        //string Crop_Text = OcrResult.Text.Replace("\r\n", "\\r\\n"); 

                         CropRes.Add(new OCRModel.CropResult
                         {
                             FormID_Key = formID[j],
                             Crop_Imgpath = croppath.ToString(),
                             Crop_Text = OcrResult.ToString().Replace("\r\n", "\\r\\n")
                         });

                        // It is desirable release this resource too.
                        croppedImage.Dispose();
                        j++;
                    }
                }
            }

            //Insert CropImage to DB
            imgpath.InsertCropResult(CropRes);
        }

        public void cropImage_Convert(List<OCRModel.Position> CropPos, string imagePath, string name, int formID)
        {
            OCRModel imgpath = new OCRModel();
            OCRModel obj = new OCRModel();
            var CropRes = new List<OCRModel.CropResult>();

            string croppath = "";
            Bitmap croppedImage;

            for (int i = 0; i < CropPos.Count; i++)
            {
                // Here we capture the resource - image file.
                using (var originalImage = new Bitmap(imagePath))
                {
                    //Set Position {x1,y1,width,height}
                    Rectangle crop = new Rectangle(CropPos[i].pos_X1, CropPos[i].pos_Y1, CropPos[i].pos_width, CropPos[i].pos_height);

                    // Here we capture another resource.
                    croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);

                }// Here we release the original resource - bitmap in memory and file on disk.

                // At this point the file on disk already free - you can record to the same path.
                //croppedImage.Save(@"C:\Users\kazarboys\Source\Repos\How-to-use-tesseract-ocr-4.0-with-csharp\tesseract-master.1153\samples\crop.jpg", ImageFormat.Jpeg);
                croppath = Path.Combine(Server.MapPath("~/Images/Crop"), name + "" + formID + "_crop_" + i + ".jpg");
                croppedImage.Save(croppath, ImageFormat.Jpeg);

                var Ocr = new AutoOcr();
                var OcrResult = Ocr.Read(croppath);
                Console.WriteLine(OcrResult.Text);

                //string Crop_Text = OcrResult.Text.Replace("\r\n", "\\r\\n"); 

                CropRes.Add(new OCRModel.CropResult
                {
                    FormID_Key = formID,
                    Crop_Imgpath = croppath.ToString(),
                    Crop_Text = OcrResult.ToString().Replace("\r\n", "\\r\\n")
                });

                // It is desirable release this resource too.
                croppedImage.Dispose();
            }

            imgpath.InsertCropResult(CropRes);

        }

    }
}