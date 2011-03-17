/* Poly2Tri
 * Copyright (c) 2009-2010, Poly2Tri Contributors
 * http://code.google.com/p/poly2tri/
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 * * Neither the name of Poly2Tri nor the names of its contributors may be
 *   used to endorse or promote products derived from this software without specific
 *   prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Poly2Tri;


namespace SwfTest
{
    [System.ComponentModel.DesignerCategory("")]
    class TestForm : Form
    {
        static readonly int kImageWidth = 64;
        
        // for some reason my computer mouse wheel gives me +/- 120 for each move of the mouse wheel.  Not sure if this 
        // is something unique to my computer, but not sure where to query the OS to get the correct divider.  Feel free
        // to change the 120.0f if you know.
        static readonly float kZoomIncrement = 0.1f / 60.0f;

        static readonly float kMaxZoomLevel = 30.0f;
        static readonly float kMinZoomLevel = 0.1f;

        private List<ITriangulatable> DisplayObjects = new List<ITriangulatable>();
        private int mDisplayIndex = 0;
        private Timer rotation;
        private bool mbPaused = false;
        private PointF mZoomPoint = new PointF(0.0f, 0.0f);
        private float mZoomLevel = 1.0f;
        private bool mIsDragging = false;
        private PointF mDragStart = new PointF(0.0f, 0.0f);
        private PointF mMouseImageCoord = new PointF(0.0f, 0.0f);

        public TestForm()
        {
            ClientSize = new Size(1000, 1000);
            DoubleBuffered = true;
            Text = "Just a test";
            Visible = true;

            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(TestForm_OnKeyPress);
            this.KeyDown += new KeyEventHandler(TestForm_OnKeyDown);
            this.MouseWheel += new MouseEventHandler(TestForm_OnMouseWheel);
            this.MouseDown += new MouseEventHandler(TestForm_OnMouseDown);
            this.MouseUp += new MouseEventHandler(TestForm_OnMouseUp);
            this.MouseMove += new MouseEventHandler(TestForm_OnMouseMove);
            this.MouseDoubleClick += new MouseEventHandler(TestForm_OnMouseDoubleClick);

            foreach (var ps in ExampleData.PointSets)
            {
                DisplayObjects.Add(ps);
            }
            foreach (var p in ExampleData.Polygons)
            {
                DisplayObjects.Add(p);
            }

            foreach (ITriangulatable obj in DisplayObjects)
            {
                try
                {
                    P2T.Triangulate(TriangulationAlgorithm.DTSweep, obj);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }

            rotation = new Timer()
                {
                    Enabled = true,
                    Interval = 500,
                };
            rotation.Tick += (o, e) =>
            {
                if (!mbPaused)
                {
                    mDisplayIndex = (mDisplayIndex + 1) % DisplayObjects.Count;
                    ResetZoomAndPan();
                }
            };

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ITriangulatable t = DisplayObjects[mDisplayIndex];

            Text = "poly2tri test: " + t.FileName;

            float xmin = (float)t.MinX;
            float xmax = (float)t.MaxX;
            float ymin = (float)t.MinY;
            float ymax = (float)t.MaxY;

            var fx = e.Graphics;

            {
                Font textFont = new Font("Times New Roman", 12);
                System.Drawing.Brush textBrush = System.Drawing.Brushes.Black;
                fx.DrawString("space = Pause/Unpause", textFont, textBrush, new PointF(10.0f, 10.0f));
                fx.DrawString("left/right arrow (while paused) = prev/next example", textFont, textBrush, new PointF(10.0f, 30.0f));
                fx.DrawString("mouse wheel = zoom in/out", textFont, textBrush, new PointF(10.0f, 50.0f));
                fx.DrawString("drag = pan", textFont, textBrush, new PointF(10.0f, 70.0f));
                fx.DrawString("double-click = reset zoom/pan", textFont, textBrush, new PointF(10.0f, 90.0f));
            }

            if (mbPaused)
            {
                Font textFont = new Font("Times New Roman", 24);
                System.Drawing.Brush textBrush = System.Drawing.Brushes.Black;
                fx.DrawString("Paused", textFont, textBrush, new PointF((ClientSize.Width / 2.0f) - 20.0f, 10.0f));
            }

            if (xmin < xmax && ymin < ymax)
            {
                float w = xmax - xmin;
                float h = ymax - ymin;
                Point2D ctr = new Point2D((double)ClientSize.Width / 2.0, (double)ClientSize.Height / 2.0);
                Point2D screenScaler = new Point2D((double)ClientSize.Width / (double)w, (double)ClientSize.Height / (double)h);
                Point2D zoomPoint = new Point2D(-mZoomPoint.X, mZoomPoint.Y);
                zoomPoint.RotateDegrees(t.DisplayRotate);
                zoomPoint.Scale(1.0 / screenScaler.X, 1.0 / screenScaler.Y);
                float zoom = mZoomLevel * (float)Math.Min(screenScaler.X, screenScaler.Y);

                fx.TranslateTransform(ctr.Xf, ctr.Yf);
                fx.RotateTransform(t.DisplayRotate);
                fx.ScaleTransform(zoom, -zoom);
                fx.TranslateTransform((-(xmax + xmin) / 2.0f) - zoomPoint.Xf, (-(ymax + ymin) / 2.0f) - zoomPoint.Yf);

                var penConstrained = new Pen(Color.Red, 1.0f / zoom);
                //var penDelaunay = new Pen(Color.Blue, 1.0f / zoom);
                var penNormal = new Pen(Color.Silver, 1.0f / zoom);
                var penErrorCase1 = new Pen(Color.Purple, 1.0f / zoom);
                var penErrorCase2 = new Pen(Color.Cyan, 1.0f / zoom);
                foreach (var tri in t.Triangles)
                {
                    PointF[] pts = new PointF[]
                    {
                        new PointF(tri.Points[0].Xf, tri.Points[0].Yf),
                        new PointF(tri.Points[1].Xf, tri.Points[1].Yf),
                        new PointF(tri.Points[2].Xf, tri.Points[2].Yf),
                    };
                    for (int i = 0; i < 3; ++i)
                    {
                        if (t.DisplayFlipX)
                        {
                            pts[i].X = xmax - (pts[i].X - xmin);
                        }
                        if (t.DisplayFlipY)
                        {
                            pts[i].Y = ymax - (pts[i].Y - ymin);
                        }
                    }
                    for (int i = 0; i < 3; ++i)
                    {
                        var curPen = penNormal;
                        DTSweepConstraint edge = null;
                        bool isConstrained = tri.GetConstrainedEdgeCCW(tri.Points[i]);
                        bool hasConstrainedEdge = tri.GetEdgeCCW(tri.Points[i], out edge);
                        if (isConstrained || hasConstrainedEdge)
                        {
                            if (isConstrained && hasConstrainedEdge)
                            {
                                curPen = penConstrained;
                            }
                            else if (isConstrained && !hasConstrainedEdge)
                            {
                                // this will happen when edges are split and is expected
                                //curPen = penErrorCase1;
                                curPen = penConstrained;
                            }
                            else
                            {
                                curPen = penErrorCase2;
                            }
                        }
                        //else if (tri.GetDelaunayEdgeCCW(tri.Points[i]))
                        //{
                        //    curPen = penDelaunay;
                        //}
                        fx.DrawLine(curPen, pts[i], pts[(i + 1) % 3]);
                    }
                }
                fx.ResetTransform();

                {
                    Point2D imageMouseCoord = new Point2D(mMouseImageCoord.X, (double)ClientSize.Height - mMouseImageCoord.Y);
                    imageMouseCoord.Subtract(ctr);
                    imageMouseCoord.RotateDegrees(t.DisplayRotate);
                    imageMouseCoord.Scale(1.0 / mZoomLevel);
                    imageMouseCoord.Scale(1.0 / screenScaler.X, 1.0 / screenScaler.Y);
                    imageMouseCoord.Translate((w / 2.0) + zoomPoint.X, (h / 2.0) + zoomPoint.Y);
                    if (t.DisplayFlipX)
                    {
                        imageMouseCoord.X = (double)xmax - (imageMouseCoord.X - (double)xmin);
                    }
                    if (t.DisplayFlipY)
                    {
                        imageMouseCoord.Y = (double)ymax - (imageMouseCoord.Y - (double)ymin);
                    }

                    Font textFont = new Font("Times New Roman", 12);
                    System.Drawing.Brush textBrush = System.Drawing.Brushes.Black;
                    fx.DrawString("Image X: " + imageMouseCoord.X.ToString(), textFont, textBrush, new PointF(20.0f, ClientSize.Height - 40.0f));
                    fx.DrawString("Image Y: " + imageMouseCoord.Y.ToString(), textFont, textBrush, new PointF(20.0f, ClientSize.Height - 20.0f));
                    fx.DrawString("ZoomPoint X: " + zoomPoint.X.ToString(), textFont, textBrush, new PointF(300.0f, ClientSize.Height - 40.0f));
                    fx.DrawString("ZoomPoint Y: " + zoomPoint.Y.ToString(), textFont, textBrush, new PointF(300.0f, ClientSize.Height - 20.0f));
                    fx.DrawString("ZoomLevel: " + mZoomLevel.ToString(), textFont, textBrush, new PointF(ClientSize.Width - 150.0f, ClientSize.Height - 20.0f));

                    //fx.DrawString("MX: " + mMouseImageCoord.X.ToString(), textFont, textBrush, new PointF(600.0f, ClientSize.Height - 40.0f));
                    //fx.DrawString("MY: " + mMouseImageCoord.Y.ToString(), textFont, textBrush, new PointF(600.0f, ClientSize.Height - 20.0f));
                }
            }

            fx.DrawImage(ExampleData.Logo256x256, ClientSize.Width - kImageWidth - 10, 10, kImageWidth, kImageWidth);

            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
            base.OnResize(e);
        }


        void TestForm_OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                mbPaused = !mbPaused;
                ResetZoomAndPan();
            }
        }


        void TestForm_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (mbPaused)
            {
                if (e.KeyCode == Keys.Left)
                {
                    if (mDisplayIndex == 0)
                    {
                        mDisplayIndex = DisplayObjects.Count - 1;
                    }
                    else
                    {
                        mDisplayIndex--;
                    }
                    ResetZoomAndPan();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    mDisplayIndex = (mDisplayIndex + 1) % DisplayObjects.Count;
                    ResetZoomAndPan();
                }
            }
        }


        void TestForm_OnMouseWheel(object sender, MouseEventArgs e)
        {
            //Console.WriteLine("e.Delta = " + e.Delta.ToString());
            float zinc = kZoomIncrement * (float)e.Delta;
            if (mZoomLevel == 1.0f)
            {
                mZoomLevel = Math.Max(Math.Min(mZoomLevel + zinc, kMaxZoomLevel), kMinZoomLevel);
            }
            else if (mZoomLevel < 1.0f)
            {
                if (mZoomLevel + zinc > 1.0f)
                {
                    mZoomLevel = 1.0f;
                }
                else
                {
                    mZoomLevel = Math.Max(Math.Min(mZoomLevel + (zinc/3.0f), kMaxZoomLevel), kMinZoomLevel);
                }
            }
            else
            {
                if (mZoomLevel + zinc < 1.0f)
                {
                    mZoomLevel = 1.0f;
                }
                else
                {
                    mZoomLevel = Math.Max(Math.Min(mZoomLevel + (zinc * 2.5f), kMaxZoomLevel), kMinZoomLevel);
                }
            }
            Invalidate();
        }


        void TestForm_OnMouseDown(object sender, MouseEventArgs e)
        {
            if (mbPaused && !mIsDragging && e.Button == MouseButtons.Left)
            {
                mIsDragging = true;
                mDragStart.X = (float)e.X;
                mDragStart.Y = (float)e.Y;
                //Console.WriteLine("Begin Dragging : DragStart = [" + mDragStart.X.ToString() + ", " + mDragStart.Y.ToString() + "]");
            }
        }


        void TestForm_OnMouseMove(object sender, MouseEventArgs e)
        {
            mMouseImageCoord.X = (float)e.X;
            mMouseImageCoord.Y = (float)e.Y;
            if (mbPaused && mIsDragging)
            {
                float dx = mMouseImageCoord.X - mDragStart.X;
                float dy = mMouseImageCoord.Y - mDragStart.Y;
                mZoomPoint.X += dx / mZoomLevel;
                mZoomPoint.Y += dy / mZoomLevel;
                mDragStart.X = mMouseImageCoord.X;
                mDragStart.Y = mMouseImageCoord.Y;
                //Console.WriteLine("Dragging : mZoomPoint = [" + mZoomPoint.X.ToString() + ", " + mZoomPoint.Y.ToString() + "]");
            }
            Invalidate();
        }


        void TestForm_OnMouseUp(object sender, MouseEventArgs e)
        {
            if (mbPaused && mIsDragging && e.Button == MouseButtons.Left)
            {
                float dx = (float)e.X - mDragStart.X;
                float dy = (float)e.Y - mDragStart.Y;
                mZoomPoint.X += (float)dx / mZoomLevel;
                mZoomPoint.Y += (float)dy / mZoomLevel;
                mDragStart.X = mDragStart.Y = 0.0f;
                mIsDragging = false;
                //Console.WriteLine("End Dragging : mZoomPoint = [" + mZoomPoint.X.ToString() + ", " + mZoomPoint.Y.ToString() + "]");
                Invalidate();
            }
        }


        void TestForm_OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (mbPaused && e.Button == MouseButtons.Left)
            {
                ResetZoomAndPan();
            }
        }


        public void ResetZoomAndPan()
        {
            mZoomLevel = 1.0f;
            mZoomPoint.X = mZoomPoint.Y = 0.0f;
            mDragStart.X = mDragStart.Y = 0.0f;
            mIsDragging = false;
            //Console.WriteLine("Zoom/Pan Reset");
            Invalidate();
        }


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestForm());
        }
    }
}
