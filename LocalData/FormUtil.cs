using LocalData.MySql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LocalData
{
   public class FormUtil
    {
        delegate void lableShowDelegate(Label lable, string strshow,Color color);

        delegate void UpdataSourceDelegate(DataGridView view, List<RecTrans> list);

        /// <summary>
        /// 修改界面提示文字
        /// </summary>
        /// <param name="lable"></param>
        /// <param name="strshow"></param>
        public static void ModifyLable(Label lable, string strshow, Color color)
        {
            if (lable.InvokeRequired)
            {
                lable.Invoke(new lableShowDelegate(ModifyLable), new object[] { lable, strshow, color });
            }
            else
            {
                lable.Text = strshow;
                lable.ForeColor =color;
            }
        }

        public static void UpdataSource(DataGridView view, List<RecTrans> list)
        {
            if (view.InvokeRequired)
            {
                view.Invoke(new UpdataSourceDelegate(UpdataSource), new object[] { view, list });
            }
            else
            {
                view.DataSource = list;
            }
        }


    }
}
