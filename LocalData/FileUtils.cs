using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalData
{
    public class FileUtils
    {
        /// <summary>
        /// 判断文件夹是否存在
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="IsCreat">若不存在是否创建，创建成功后返回true</param>
        /// <returns></returns>
        public static bool DirExit(string path, bool IsCreat)
        {
            if (!Directory.Exists(path))
            {
                if (IsCreat)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch (Exception e)
                    {
                        LogHelper.WriteLog("文件夹创建错误", e);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}