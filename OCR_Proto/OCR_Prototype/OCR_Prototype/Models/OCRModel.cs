using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace OCR_Prototype.Models
{
    public class OCRModel
    {

        public class Position
        {
            public int page { get; set; }
            public int pos_X1 { get; set; }
            public int pos_Y1 { get; set; }
            public int pos_width { get; set; }
            public int pos_height { get; set; }
        }

        public class CropResult
        {
            public int FormID_Key { get; set; }
            public string Crop_Imgpath { get; set; }
            public string Crop_Text { get; set; }
        }

        public class Listing
        {
            public int ID { get; set; }
            public string Reference { get; set; }
        }

        public class getDetails
        { 
            public int ID { get; set; }
            public string Reference { get; set; }
            public string Form_Path { get; set; }
            public string Crop_Imgpath { get; set; }
            public string Crop_Text { get; set; }
        }

        private SqlConnection con;

        private void connection()
        {
            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);
        }

        public string getseqnum()
        {
            string sql = null;
            string sqlseq = null;
            int seqnum = 0;
            string newnum = null;
            string retseq = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlseq = "SELECT VALUE FROM FORM_COMMON WHERE REFERENCE ='Ref_Seq'";

                    using (SqlCommand cmd = new SqlCommand(sqlseq, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            seqnum = Convert.ToInt32(reader.GetString(0));
                            retseq = seqnum.ToString("0000");
                            seqnum += 1;
                            newnum = seqnum.ToString("0000");  
                        }
                        reader.Close();
                    }

                    sql = "UPDATE FORM_COMMON set VALUE = '"+newnum+ "' WHERE REFERENCE = 'Ref_Seq'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@val", "OI-" + newnum);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch(Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
            return retseq;
        }

        public List<int> insertForm(List<string> formpath,int formID)
        {
            DateTime date = DateTime.Now;
            string sql = null;
            string seq = getseqnum(); ;

            List<int> getid = new List<int>();

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();

                    for (int i = 0; i < formpath.Count; i++)
                    {
                        
                        sql = "INSERT INTO Form_Image VALUES (@ImgId,@ImgRef,@Imgpath,@Cre_Date,@Cre_By); SELECT SCOPE_IDENTITY()";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ImgId", formID);
                            cmd.Parameters.AddWithValue("@ImgRef", "OI-" + seq);
                            cmd.Parameters.AddWithValue("@Imgpath", formpath[i]);
                            cmd.Parameters.AddWithValue("@Cre_Date", date);
                            cmd.Parameters.AddWithValue("@Cre_By", "SCRIPT");
                            //cmd.ExecuteNonQuery();

                            getid.Add(Convert.ToInt32(cmd.ExecuteScalar()));
                        }
                    }
                }
                catch(Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
            return getid;
        }

        public List<Position> retrieveBoxPos(int DocID)
        {
            var BoxID = new List<Position>(); 
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT A.[Page_No],B.[pos_x1],B.[pos_y1],B.[pos_x2],B.[pos_y2] FROM [Form_Config] A INNER JOIN [FormBox_Position] B ON A.[BoxPosition_ID] = B.[ID] AND A.[Doc_Key] = '" + DocID + "'";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            BoxID.Add(new Position
                            {
                                page = reader.GetInt32(0),
                                pos_X1 = reader.GetInt32(1),
                                pos_Y1 = reader.GetInt32(2),
                                pos_width = reader.GetInt32(3),
                                pos_height = reader.GetInt32(4)
                            });
                        }
                        reader.Close();
                    }               
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                    
            }
            return BoxID;
        }

        public void InsertCropResult(List<CropResult> CropImageRes)
        {
            DateTime date = DateTime.Now;
            string sql = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();

                    for (int i = 0; i < CropImageRes.Count; i++)
                    {
                        sql = "INSERT INTO Form_ImageCrop VALUES (@FormKey,@Imgpath,@ImgText,@Cre_Date,@Cre_By)";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@FormKey", CropImageRes[i].FormID_Key);
                            cmd.Parameters.AddWithValue("@Imgpath", CropImageRes[i].Crop_Imgpath);
                            cmd.Parameters.AddWithValue("@ImgText", CropImageRes[i].Crop_Text);
                            cmd.Parameters.AddWithValue("@Cre_Date", date);
                            cmd.Parameters.AddWithValue("@Cre_By", "SCRIPT");
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
        }

        public List<Listing> getListing()
        {
            var ListID = new List<Listing>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT [ID],[Form_Reference] FROM [Form_Image]";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            ListID.Add(new Listing
                            {
                                ID = reader.GetInt32(0),
                                Reference = reader.GetString(1)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                return ListID;
            }
        }

        public List<getDetails> getDetailList(string FormImgid)
        {
            var DetailList= new List<getDetails>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT A.[ID],A.[Form_Reference],A.[Form_Path],B.[Crop_Imgpath],B.[Crop_Text] FROM [Form_Image] A INNER JOIN [Form_ImageCrop] B ON A.ID = B.FormID_key where A.Form_Reference = '" + FormImgid+"'";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            DetailList.Add(new getDetails
                            {
                                ID = reader.GetInt32(0),
                                Reference = reader.GetString(1),
                                Form_Path = reader.GetString(2),
                                Crop_Imgpath = reader.GetString(3),
                                Crop_Text = reader.GetString(4)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                return DetailList;
            }
        }

    }
}