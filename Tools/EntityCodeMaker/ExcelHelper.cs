﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using NPOI;
using NPOI.HPSF;
using NPOI.HSSF;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.POIFS;
using NPOI.Util;
using NPOI.SS.Util;

namespace CodeMaker.Common
{
    /// <summary>
    /// 导出Excel
    /// </summary>
    public class ExcelHelper
    {
        /// <summary>  
        /// DataTable导出到Excel文件  
        /// </summary>  
        /// <param name="dtSource">源DataTable</param>  
        /// <param name="strHeaderText">表头文本</param>  
        /// <param name="strFileName">保存位置</param>  
        /// <Author></Author>  
        public static void Export(DataTable dtSource, string strHeaderText, string strFileName)
        {
            using (MemoryStream ms = Export(dtSource, strHeaderText, new Dictionary<string, string>(),new Dictionary<string, string>()))
            {
                using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }
        /// <summary>
        /// DataTable导出到Excel文件，可以自定义导出那些列
        /// </summary>
        /// <param name="dtSource"></param>
        /// <param name="strHeaderText"></param>
        /// <param name="columnNames"></param>
        /// <param name="strFileName"></param>
        public static void Export(DataTable dtSource, string strHeaderText, Dictionary<string, string> columnNames, string strFileName)
        {
            using (MemoryStream ms = Export(dtSource, strHeaderText, columnNames,new Dictionary<string, string>()))
            {
                using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }
        /// <summary>
        ///  DataTable导出到Excel文件，可以自定义导出列和设置列的数据格式
        /// </summary>
        /// <param name="dtSource"></param>
        /// <param name="strHeaderText"></param>
        /// <param name="columnNames"></param>
        /// <param name="dataformats"></param>
        /// <param name="strFileName"></param>
        public static void Export(DataTable dtSource, string strHeaderText, Dictionary<string, string> columnNames, Dictionary<string, string> dataformats, string strFileName)
        {
            using (MemoryStream ms = Export(dtSource, strHeaderText, columnNames, dataformats))
            {
                using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }


        /// <summary>  
        /// DataTable导出到Excel的MemoryStream  
        /// </summary>  
        /// <param name="dtSource">源DataTable</param>  
        /// <param name="strHeaderText">表头文本</param>  
        /// <Author> 2010-5-8 22:21:41</Author>  
        public static MemoryStream Export(DataTable dtSource, string strHeaderText, Dictionary<string, string> columnNames,Dictionary<string,string> dataformats)
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            HSSFSheet sheet = (HSSFSheet)workbook.CreateSheet();

            #region 右击文件 属性信息
            {
                DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "";
                workbook.DocumentSummaryInformation = dsi;

                SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                si.Author = "kakake"; //填加xls文件作者信息  
                si.ApplicationName = ""; //填加xls文件创建程序信息  
                si.LastAuthor = ""; //填加xls文件最后保存者信息  
                si.Comments = "说明信息"; //填加xls文件作者信息  
                si.Title = ""; //填加xls文件标题信息  
                si.Subject = "";//填加文件主题信息  
                si.CreateDateTime = DateTime.Now;
                workbook.SummaryInformation = si;
            }
            #endregion

            HSSFCellStyle dateStyle = (HSSFCellStyle)workbook.CreateCellStyle();
            HSSFDataFormat format = (HSSFDataFormat)workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

            HSSFCellStyle customStyle = (HSSFCellStyle)workbook.CreateCellStyle();
            HSSFDataFormat customformat = (HSSFDataFormat)workbook.CreateDataFormat();

            //取得列宽  
            int[] arrColWidth = new int[dtSource.Columns.Count];
            foreach (DataColumn item in dtSource.Columns)
            {
                arrColWidth[item.Ordinal] = Encoding.GetEncoding(936).GetBytes(item.ColumnName.ToString()).Length;
            }
            for (int i = 0; i < dtSource.Rows.Count; i++)
            {
                for (int j = 0; j < dtSource.Columns.Count; j++)
                {
                    int intTemp = Encoding.GetEncoding(936).GetBytes(dtSource.Rows[i][j].ToString()).Length;
                    if (intTemp > arrColWidth[j])
                    {
                        arrColWidth[j] = intTemp;
                    }
                }
            }



            int rowIndex = 0;
            int index = 0;
            foreach (DataRow row in dtSource.Rows)
            {
                #region 新建表，填充表头，填充列头，样式
                if (rowIndex == 65535 || rowIndex == 0)
                {
                    if (rowIndex != 0)
                    {
                        sheet = (HSSFSheet)workbook.CreateSheet();
                    }

                    #region 表头及样式
                    {
                        HSSFRow headerRow = (HSSFRow)sheet.CreateRow(0);
                        headerRow.HeightInPoints = 25;
                        headerRow.CreateCell(0).SetCellValue(strHeaderText);

                        HSSFCellStyle headStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                        headStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.CENTER;
                        HSSFFont font = (HSSFFont)workbook.CreateFont();
                        font.FontHeightInPoints = 20;
                        font.Boldweight = 700;
                        headStyle.SetFont(font);

                        headerRow.GetCell(0).CellStyle = headStyle;
                        if (columnNames.Count > 0)
                        {
                            sheet.AddMergedRegion(new Region(0, 0, 0, columnNames.Count - 1));
                        }
                        else
                            sheet.AddMergedRegion(new Region(0, 0, 0, dtSource.Columns.Count - 1));
                        //headerRow.Dispose();
                    }
                    #endregion


                    #region 列头及样式
                    {
                        HSSFRow headerRow = (HSSFRow)sheet.CreateRow(1);


                        HSSFCellStyle headStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                        headStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.CENTER;
                        HSSFFont font = (HSSFFont)workbook.CreateFont();
                        font.FontHeightInPoints = 10;
                        font.Boldweight = 700;
                        headStyle.SetFont(font);

                        index = 0;
                        foreach (DataColumn column in dtSource.Columns)
                        {
                            if (columnNames.Count > 0)
                            {
                                if (columnNames.ContainsKey(column.ColumnName))
                                {

                                    headerRow.CreateCell(index).SetCellValue(columnNames[column.ColumnName]);
                                    headerRow.GetCell(index).CellStyle = headStyle;

                                    //设置列宽  
                                    sheet.SetColumnWidth(index, (Encoding.GetEncoding(936).GetBytes(columnNames[column.ColumnName]).Length + 1) * 256);

                                    index++;
                                }
                            }
                            else
                            {
                                headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
                                headerRow.GetCell(column.Ordinal).CellStyle = headStyle;

                                //设置列宽  
                                sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256);
                            }
                        }
                        //headerRow.Dispose();
                    }
                    #endregion

                    rowIndex = 2;
                }
                #endregion


                #region 填充内容
                index = 0;
                HSSFRow dataRow = (HSSFRow)sheet.CreateRow(rowIndex);
                foreach (DataColumn column in dtSource.Columns)
                {
                    if (columnNames.Count > 0)
                    {
                        if (columnNames.ContainsKey(column.ColumnName))
                        {
                            HSSFCell newCell = (HSSFCell)dataRow.CreateCell(index);

                            string drValue = row[column].ToString();

                            switch (column.DataType.ToString())
                            {
                                case "System.String"://字符串类型  
                                    newCell.SetCellValue(drValue);
                                    break;
                                case "System.DateTime"://日期类型  
                                    DateTime dateV;
                                    DateTime.TryParse(drValue, out dateV);
                                    newCell.SetCellValue(dateV);

                                    newCell.CellStyle = dateStyle;//格式化显示  
                                    break;
                                case "System.Boolean"://布尔型  
                                    bool boolV = false;
                                    bool.TryParse(drValue, out boolV);
                                    newCell.SetCellValue(boolV);
                                    break;
                                case "System.Int16"://整型  
                                case "System.Int32":
                                case "System.Int64":
                                case "System.Byte":
                                    int intV = 0;
                                    int.TryParse(drValue, out intV);
                                    newCell.SetCellValue(intV);
                                    break;
                                case "System.Decimal"://浮点型  
                                case "System.Double":
                                    double doubV = 0;
                                    double.TryParse(drValue, out doubV);
                                    newCell.SetCellValue(doubV);
                                    //HSSFCellStyle celldoubleStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                                    //int pos = Convert.ToString(doubV).Length - Convert.ToString(doubV).IndexOf('.') - 1;
                                    //if (pos == 4)
                                    //{
                                    //    celldoubleStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.0000");
                                    //}
                                    //else if (pos == 2)
                                    //{
                                    //    celldoubleStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
                                    //}
                                    //newCell.CellStyle = celldoubleStyle;
                                    break;
                                case "System.DBNull"://空值处理  
                                    newCell.SetCellValue("");
                                    break;
                                default:
                                    newCell.SetCellValue("");
                                    break;
                            }

                            if (dataformats.ContainsKey(column.ColumnName))
                            {
                                customStyle.DataFormat = customformat.GetFormat(dataformats[column.ColumnName]);
                                newCell.CellStyle = customStyle;
                            }

                            index++;
                        }
                    }
                    else
                    {
                        HSSFCell newCell = (HSSFCell)dataRow.CreateCell(column.Ordinal);

                        string drValue = row[column].ToString();

                        switch (column.DataType.ToString())
                        {
                            case "System.String"://字符串类型  
                                newCell.SetCellValue(drValue);
                                break;
                            case "System.DateTime"://日期类型  
                                DateTime dateV;
                                DateTime.TryParse(drValue, out dateV);
                                newCell.SetCellValue(dateV);

                                newCell.CellStyle = dateStyle;//格式化显示  
                                break;
                            case "System.Boolean"://布尔型  
                                bool boolV = false;
                                bool.TryParse(drValue, out boolV);
                                newCell.SetCellValue(boolV);
                                break;
                            case "System.Int16"://整型  
                            case "System.Int32":
                            case "System.Int64":
                            case "System.Byte":
                                int intV = 0;
                                int.TryParse(drValue, out intV);
                                newCell.SetCellValue(intV);
                                break;
                            case "System.Decimal"://浮点型  
                            case "System.Double":
                                double doubV = 0;
                                double.TryParse(drValue, out doubV);
                                newCell.SetCellValue(doubV);
                                //HSSFCellStyle celldoubleStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                                //int pos = Convert.ToString(doubV).Length - Convert.ToString(doubV).IndexOf('.') - 1;
                                //if (pos == 4)
                                //{
                                //    celldoubleStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.0000");
                                //}
                                //else if (pos == 2)
                                //{
                                //    celldoubleStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
                                //}
                                //newCell.CellStyle = celldoubleStyle;
                                break;
                            case "System.DBNull"://空值处理  
                                newCell.SetCellValue("");
                                break;
                            default:
                                newCell.SetCellValue("");
                                break;
                        }

                        if (dataformats.ContainsKey(column.ColumnName))
                        {
                            customStyle.DataFormat = customformat.GetFormat(dataformats[column.ColumnName]);
                            newCell.CellStyle = customStyle;
                        }
                    }
                }
                #endregion

                rowIndex++;
            }


            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                ms.Flush();
                ms.Position = 0;

                //sheet.Workbook.Dispose();
                //workbook.Dispose();//一般只用写这一个就OK了，他会遍历并释放所有资源，但当前版本有问题所以只释放 sheet  
                return ms;
            }

        }

    }
}