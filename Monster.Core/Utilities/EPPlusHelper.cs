﻿using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Monster.Core.DBManager;
using Monster.Core.Extensions;
using Monster.Core.Infrastructure;
using Monster.Entity.DomainModels;

namespace Monster.Core.Utilities
{
    public class EPPlusHelper
    {
        /// <summary>
        /// 导入模板(仅限框架导出模板使用)(202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="exportColumns">指定导出的列</param>
        /// <param name="ignoreColumns">忽略不导出的列(如果设置了exportColumns,ignoreColumns不会生效)</param>
        /// <returns></returns>

        public static WebResponseContent ReadToDataTable<T>(string path, Expression<Func<T, object>> exportColumns = null, List<string> ignoreTemplate = null)
        {
            WebResponseContent responseContent = new WebResponseContent();

            FileInfo file = new FileInfo(path);
            if (!file.Exists) return responseContent.Error("未找到上传的文件,请重新上传");

            List<T> entities = new List<T>();
            using (ExcelPackage package = new ExcelPackage(file))
            {
                if (package.Workbook.Worksheets.Count == 0 ||
                    package.Workbook.Worksheets.FirstOrDefault().Dimension.End.Row <= 1)
                    return responseContent.Error("未导入数据");
                List<CellOptions> cellOptions = GetExportColumnInfo(typeof(T).GetEntityTableName(), false, false);
                //设置忽略的列
                if (exportColumns != null)
                {
                    cellOptions = cellOptions
                        .Where(x => exportColumns.GetExpressionToArray().Select(s => s.ToLower()).Contains(x.ColumnName.ToLower()))
                        .ToList();
                }
                else if (ignoreTemplate != null)
                {
                    cellOptions = cellOptions
                        .Where(x => !ignoreTemplate.Select(s => s.ToLower()).Contains(x.ColumnName.ToLower()))
                        .ToList();
                }


                ExcelWorksheet sheetFirst = package.Workbook.Worksheets.FirstOrDefault();

                for (int j = sheetFirst.Dimension.Start.Column, k = sheetFirst.Dimension.End.Column; j <= k; j++)
                {
                    string columnCNName = sheetFirst.Cells[1, j].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(columnCNName))
                    {
                        CellOptions options = cellOptions.Where(x => x.ColumnCNName == columnCNName).FirstOrDefault();
                        if (options == null)
                        {
                            return responseContent.Error("导入文件列[" + columnCNName + "]不是模板中的列");
                        }
                        if (options.Index > 0)
                        {
                            return responseContent.Error("导入文件列[" + columnCNName + "]不能重复");
                        }
                        options.Index = j;
                    }
                }
                if (cellOptions.Exists(x => x.Index == 0))
                {
                    return responseContent.Error("导入文件列必须与导入模板相同");
                }

                PropertyInfo[] propertyInfos = typeof(T).GetProperties()
                       .Where(x => cellOptions.Select(s => s.ColumnName).Contains(x.Name))
                       .ToArray();
                ExcelWorksheet sheet = package.Workbook.Worksheets.FirstOrDefault();
                for (int m = sheet.Dimension.Start.Row + 1, n = sheet.Dimension.End.Row; m <= n; m++)
                {
                    T entity = Activator.CreateInstance<T>();
                    for (int j = sheet.Dimension.Start.Column, k = sheet.Dimension.End.Column; j <= k; j++)
                    {
                        string value = sheet.Cells[m, j].Value?.ToString();

                        CellOptions options = cellOptions.Where(x => x.Index == j).FirstOrDefault();
                        PropertyInfo property = propertyInfos.Where(x => x.Name == options.ColumnName).FirstOrDefault();

                        if (options.Requierd && string.IsNullOrEmpty(value))
                        {
                            return responseContent.Error($"第{m}行[{options.ColumnCNName}]验证未通过,不能为空。");
                        }

                        //验证字典数据
                        if (!string.IsNullOrEmpty(options.DropNo))
                        {
                            string key = options.KeyValues.Where(x => x.Value == value)
                                  .Select(s => s.Key)
                                  .FirstOrDefault();
                            if (key == null)//&& options.Requierd
                            {
                                //小于20个字典项，直接提示所有可选value
                                string values = options.KeyValues.Count < 20 ? (string.Join(',', options.KeyValues.Select(s => s.Value))) : options.ColumnCNName;
                                return responseContent.Error($"第{m}行[{options.ColumnCNName}]验证未通过,必须是字典数据中[{values}]的值。");
                            }
                            //将值设置为数据字典的Key,如果导入为是/否字典项，存在表中应该对为1/0
                            value = key;
                        }

                        //验证导入与实体数据类型是否相同
                        (bool, string, object) result = property.ValidationProperty(value, options.Requierd);

                        if (!result.Item1)
                        {
                            return responseContent.Error($"第{m}行[{options.ColumnCNName}]验证未通过,{result.Item2}");
                        }

                        property.SetValue(entity, value.ChangeType(property.PropertyType));
                    }
                    entity.SetCreateDefaultVal();
                    entities.Add(entity);
                }
            }
            return responseContent.OK(null, entities);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnCNName">key为字段名, ValueTuple<string, int>为字段中文名及列宽度</param>
        /// <param name="dicNos"> List<ValueTuple<string, string, string>>item1列名,item2 字典value,item3字典name </param>
        /// <returns>返回文件保存的路径</returns>
        public static string Export(DataTable table, List<CellOptions> cellOptions, string savePath, string fileName)
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            //获取所有有值的数据源
            var dicNoKeys = cellOptions
                 .Where(x => !string.IsNullOrEmpty(x.DropNo) && x.KeyValues != null && x.KeyValues.Keys.Count > 0)
                 .Select(x => new { x.DropNo, x.ColumnName }).Distinct().ToList();



            using (ExcelPackage package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("sheet1");
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    using (ExcelRange range = worksheet.Cells[1, i + 1])
                    {
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.Gray);  //背景色
                        worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                    }
                    CellOptions options = cellOptions.Where(x => x.ColumnName == table.Columns[i].ColumnName).FirstOrDefault();
                    if (options != null)
                    {
                        worksheet.Column(i + 1).Width = options.ColumnWidth / 6.00;
                        worksheet.Cells[1, i + 1].Value = options.ColumnCNName;
                    }
                    else
                    {
                        worksheet.Column(i + 1).Width = 15;
                        worksheet.Cells[1, i + 1].Value = table.Columns[i].ColumnName;
                    }
                }

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        string cellValue = (table.Rows[i][j] ?? "").ToString();
                        if (dicNoKeys.Exists(x => x.ColumnName == table.Columns[j].ColumnName))
                        {
                            cellOptions.Where(x => x.ColumnName == table.Columns[j].ColumnName)
                                .Select(s => s.KeyValues)
                                .FirstOrDefault()
                                .TryGetValue(cellValue, out string result);
                            cellValue = result ?? cellValue;
                        }
                        worksheet.Cells[i + 2, j + 1].Value = cellValue;
                    }
                }
                package.SaveAs(new FileInfo(savePath + fileName));
            }
            return savePath + fileName;
        }


        /// <summary>
        /// 下载导出模板(仅限框架导出模板使用)(202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exportColumns">指定导出的列</param>
        /// <param name="ignoreColumns">忽略不导出的列(如果设置了exportColumns,ignoreColumns不会生效)</param>
        /// <param name="savePath">导出文件的绝对路径</param>
        /// <param name="fileName">导出的文件名+后缀,如:123.xlsx</param>
        /// <returns></returns>
        public static string ExportTemplate<T>(List<string> exportColumns, List<string> ignoreColumns, string savePath, string fileName)
        {
            return Export<T>(null, exportColumns, ignoreColumns, savePath, fileName, true);
        }

        /// <summary>
        /// 下载导出模板(仅限框架导出模板使用)(202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exportColumns">指定导出的列</param>
        /// <param name="ignoreColumns">忽略不导出的列(如果设置了exportColumns,ignoreColumns不会生效)</param>
        /// <param name="savePath">导出文件的绝对路径</param>
        /// <param name="fileName">导出的文件名+后缀,如:123.xlsx</param>
        /// <returns></returns>
        public static string ExportTemplate<T>(Expression<Func<T, object>> exportColumns, List<string> ignoreColumns, string savePath, string fileName)
        {
            return Export<T>(null, exportColumns?.GetExpressionToArray(), ignoreColumns, savePath, fileName, true);
        }

        /// <summary>
        /// 下载导出模板(仅限框架导出模板使用)(202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ignoreColumns">忽略不导出的列</param>
        /// <param name="savePath">导出文件的绝对路径</param>
        /// <param name="fileName">导出的文件名+后缀,如:123.xlsx</param>
        /// <returns></returns>
        public static string ExportTemplate<T>(List<string> ignoreColumns, string savePath, string fileName)
        {
            return Export<T>(null, null, ignoreColumns, savePath, fileName, true);
        }

        /// <summary>
        /// 导出excel文件(导入功能里的导出模板也使用的此功能，list传的null，导出的文件只有模板的标题)
        /// (202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="cellOptions">对应代码生成器的配置</param>
        /// <param name="ignore">忽略不导出的字段</param>
        /// <param name="savePath"></param>
        /// <param name="fileName"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static string Export<T>(List<T> list, Expression<Func<T, object>> ignore, string savePath, string fileName, bool template = false)
        {
            return Export(list, null, ignore?.GetExpressionProperty(), savePath, fileName, template);
        }

        /// <summary>
        /// 导出excel文件(导入功能里的导出模板也使用的此功能，list传的null，导出的文件只有模板的标题)
        /// (202.05.07)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">导出的对象</param>
        /// <param name="exportColumns">指定导出的列</param>
        /// <param name="ignoreColumns">忽略不导出的列(如果设置了exportColumns,ignoreColumns不会生效)</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="fileName">保存的文件名</param>
        ///  <param name="template">是否为下载模板</param>
        /// <returns></returns>
        public static string Export<T>(List<T> list, IEnumerable<string> exportColumns, IEnumerable<string> ignoreColumns, string savePath, string fileName, bool template = false)
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            //获取代码生成器对应的配置信息
            //  List<CellOptions> cellOptions = GetExportColumnInfo(typeof(T).GetEntityTableName(), template);
            //2020.06.02修复使用表别名时读取不到配置信息
            List<CellOptions> cellOptions = GetExportColumnInfo(typeof(T).Name, template);
            string fullPath = savePath + fileName;
            //获取所有有值的数据源
            var dicNoKeys = cellOptions
                 .Where(x => !string.IsNullOrEmpty(x.DropNo) && x.KeyValues != null && x.KeyValues.Keys.Count > 0)
                 .Select(x => new { x.DropNo, x.ColumnName }).Distinct().ToList();

            List<PropertyInfo> propertyInfo = null;

            /*导出时，代码生成器中的表配置信息Sys_TableInfo/Sys_TableColumn必须与当前数据库相同，否则导出来可能没有数据*/

            //2020.06.02优化读取导出列配置信息
            //导出指定的列
            //如果指定了导出的标题列，忽略的标题列不再起作用
            if (exportColumns != null && exportColumns.Count() > 0)
            {
                propertyInfo =
                   typeof(T).GetProperties()
                  .Where(x => exportColumns.Select(g => g.ToLower()).Contains(x.Name.ToLower())).ToList();
                //.Where(x => cellOptions.Select(s => s.ColumnName) //获取代码生成器配置的列
                //.Contains(x.Name)).ToList();
            }
            else if (ignoreColumns != null && ignoreColumns.Count() > 0)
            {
                propertyInfo = typeof(T).GetProperties()
                  .Where(x => !ignoreColumns.Select(g => g.ToLower()).Contains(x.Name.ToLower()))
                  .Where(x => cellOptions.Select(s => s.ColumnName).Contains(x.Name)) //获取代码生成器配置的列
                  .ToList();
            }
            else
            {
                //默认导出代码生成器中配置【是否显示】=是的列
                propertyInfo = typeof(T).GetProperties()
                  .Where(x => cellOptions.Select(s => s.ColumnName).Contains(x.Name)) //获取代码生成器配置的列
                  .ToList();
                /*
                 * 如果propertyInfo查出来的长度=0
                 * 1、代码生成器中的配置信息是否同步到当前数据库
                 * 2、代码生成器中的配置列名与model的字段是否大小写一致
                 */
            }
            string[] dateArr = null;
            if (!template)
            {
                dateArr = propertyInfo.Where(x => x.PropertyType == typeof(DateTime)
                || x.PropertyType == typeof(DateTime?))
                .Select(s => s.Name).ToArray();
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("sheet1");
                for (int i = 0; i < propertyInfo.Count; i++)
                {
                    string columnName = propertyInfo[i].Name;
                    using (ExcelRange range = worksheet.Cells[1, i + 1])
                    {
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        //默认灰色背景，白色字体
                        Color backgroundColor = Color.Gray;
                        //字体颜色
                        Color fontColor = Color.White;
                        //下载模板并且是必填项，将表格设置为黄色
                        if (template)
                        {
                            fontColor = Color.Black;
                            if (cellOptions.Exists(x => x.ColumnName == columnName && x.Requierd))
                            {
                                backgroundColor = Color.Yellow;  //黄色必填
                            }
                            else
                            {
                                backgroundColor = Color.White;
                            }
                        }
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(backgroundColor);  //背景色
                        worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(fontColor);//字体颜色
                    }
                    CellOptions options = cellOptions.Where(x => x.ColumnName == columnName).FirstOrDefault();
                    if (options != null)
                    {
                        worksheet.Column(i + 1).Width = options.ColumnWidth / 6.00;
                        worksheet.Cells[1, i + 1].Value = options.ColumnCNName;
                    }
                    else
                    {
                        worksheet.Column(i + 1).Width = 15;
                        worksheet.Cells[1, i + 1].Value = columnName;
                    }
                }
                //下载模板直接返回
                if (template)
                {
                    package.SaveAs(new FileInfo(fullPath));
                    return fullPath;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = 0; j < propertyInfo.Count; j++)
                    {
                        string cellValue = null;
                        if (dateArr != null && dateArr.Contains(propertyInfo[j].Name))
                        {
                            object value = propertyInfo[j].GetValue(list[i]);
                            cellValue = value == null ? "" : ((DateTime)value).ToString("yyyy-MM-dd HH:mm:sss").Replace(" 00:00:00", "");
                        }
                        else
                        {
                            cellValue = (propertyInfo[j].GetValue(list[i]) ?? "").ToString();
                        }
                        if (dicNoKeys.Exists(x => x.ColumnName == propertyInfo[j].Name))
                        {
                            cellOptions.Where(x => x.ColumnName == propertyInfo[j].Name)
                              .Select(s => s.KeyValues)
                              .FirstOrDefault()
                              .TryGetValue(cellValue, out string result);
                            cellValue = result ?? cellValue;
                        }
                        worksheet.Cells[i + 2, j + 1].Value = cellValue;
                    }
                }

                package.SaveAs(new FileInfo(fullPath));
            }
            return fullPath;
        }


        /// <summary>
        /// 获取导出的列的数据信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="temlate">是否为下载模板</param>
        /// filterKeyValue是否过滤Key相同的数据
        /// <returns></returns>
        private static List<CellOptions> GetExportColumnInfo(string tableName, bool temlate = false, bool filterKeyValue = true)
        {
            //&& x.IsDisplay == 1&&x.IsReadDataset==0只导出代码生器中设置为显示并且不是只读的列，可根据具体业务设置导出列
            // && x.IsReadDataset == 0
            //2020.06.02增加不区分大表名大小写: 原因mysql可能是表名是小写，但生成model的时候强制大写
            //x => x.TableName.ToLower() == tableName.ToLower()
            List<CellOptions> cellOptions = DBServerProvider.DbContext.Set<Sys_TableColumn>()
              .Where(x => x.TableName.ToLower() == tableName.ToLower() && x.IsDisplay == 1).Select(c => new CellOptions()
              {
                  ColumnName = c.ColumnName,
                  ColumnCNName = c.ColumnCnName,
                  DropNo = c.DropNo,
                  Requierd = c.IsNull > 0 ? false : true,
                  ColumnWidth = c.ColumnWidth ?? 90
              }).ToList();

            if (temlate) return cellOptions;

            var dicNos = cellOptions.Where(x => !string.IsNullOrEmpty(x.DropNo)).Select(c => c.DropNo);

            if (dicNos.Count() == 0) return cellOptions;

            var dictionaries = DictionaryManager.GetDictionaries(dicNos);
            //获取绑定字典数据源下拉框的值
            foreach (string dicNo in dicNos.Distinct())
            {
                Dictionary<string, string> keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                List<Sys_DictionaryList> dictionaryLists = dictionaries
                   .Where(x => x.DicNo == dicNo && x.Sys_DictionaryList != null)
                   .Select(s => s.Sys_DictionaryList).FirstOrDefault();
                if (dictionaryLists == null || dictionaryLists.Count == 0) continue;
                foreach (var item in dictionaryLists)
                {
                    ////filterKeyValue为true过滤keyvalue相不的项,key==value相同的则不处理
                    if (filterKeyValue && item.DicName == item.DicValue) continue;
                    if (keyValues.ContainsKey(item.DicValue)) continue;
                    keyValues.Add(item.DicValue, item.DicName);
                }

                foreach (CellOptions options in cellOptions.Where(x => x.DropNo == dicNo))
                {
                    options.KeyValues = keyValues;
                }
            }
            return cellOptions;
        }
    }

    public class CellOptions
    {
        public string ColumnName { get; set; }//导出表的列
        public string ColumnCNName { get; set; }//导出列的中文名
        public string DropNo { get; set; }//字典编号
        public int ColumnWidth { get; set; }//导出列的宽度,代码生成维护的宽度
        public bool Requierd { get; set; } //是否必填
        public int Index { get; set; }//列所在模板的序号(导入用)
                                      //对应字典项维护的Key,Value
        public Dictionary<string, string> KeyValues { get; set; }
        //public string Value { get; set; } //对应字典项维护的Value
        //public string Name { get; set; } //对应字典项显示的名称
    }
}
