using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ShellSquare.Client.ETP
{
   public class CustomCheckedListBox : CheckedListBox
    {
        public CustomCheckedListBox()
        {
            DoubleBuffered = true;
            ItemHeight = 25;
        }
        public override int ItemHeight { get; set; }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                e = new DrawItemEventArgs(e.Graphics,
                                          e.Font,
                                          e.Bounds,
                                          e.Index,
                                          e.State ^ DrawItemState.Selected,
                                          e.ForeColor,
                                          Color.LightGreen);
            e.DrawBackground();
            bool isChecked = GetItemChecked(e.Index);
            var b = e.Bounds;
            var state = GetItemChecked(e.Index) ? CheckBoxState.CheckedNormal : CheckBoxState.MixedNormal;
            Size glyphSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
            int checkPad = (b.Height - glyphSize.Height) / 2;
            var pt = new Point(b.X + checkPad, b.Y + checkPad);

            Rectangle rect = new Rectangle(pt, new Size(14, 14));
            e.Graphics.DrawRectangle(Pens.Black, rect);

            if (state == CheckBoxState.CheckedNormal)
            {
                using (SolidBrush brush = new SolidBrush(ForeColor))
                using (Font wing = new Font("Wingdings", 10f, FontStyle.Bold))
                    e.Graphics.DrawString("ü", wing, brush, pt.X - 1, pt.Y - 1); 
            }

            using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                using (Brush brush = new SolidBrush(isChecked ? CheckedItemColor : ForeColor))
                {
                    e.Graphics.DrawString(Items[e.Index].ToString(), Font, brush, new Rectangle(e.Bounds.Height, e.Bounds.Top + 2, e.Bounds.Width - e.Bounds.Height, e.Bounds.Height), sf);
                }
            }
            e.DrawFocusRectangle();

        }
        Color checkedItemColor = Color.Black;
        public Color CheckedItemColor
        {
            get { return checkedItemColor; }
            set
            {
                checkedItemColor = value;
                Invalidate();
            }
        }

    }
}
