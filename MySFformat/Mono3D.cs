﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace MySFformat
{
    enum RenderMode { Line, Triangle, Both, BothNoTex, TexOnly, Weights }

    public class MeshInfos 
    {
        public VertexPositionColorTexture[] triTextureVertices = new VertexPositionColorTexture[0];
        public string textureName = "";
    }
    class Mono3D : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D test;
        KeyboardState prevState = new KeyboardState();
        MouseState prevMState = new MouseState();
        public RenderMode renderMode = RenderMode.Both;
        public VertexPositionColor[] vertices = new VertexPositionColor[0];
        public VertexPositionColor[] triVertices = new VertexPositionColor[0];
        public VertexPositionColor[] triWeightVertices = new VertexPositionColor[0];
        public bool flatShading = false;
        public int selectedBone = 0;
        public float weightToPaint = 0.0f;
        public bool paintingWeight = false;

        //  public VertexPositionColorTexture[] triTextureVertices = new VertexPositionColorTexture[0];
        public MeshInfos[] meshInfos = new MeshInfos[0];
        VertexPositionTexture[] floorVerts;
        BasicEffect effect;

        float mouseX, mouseY;

        float cameraX = 0;
        float cameraY = 4;
        float cameraZ = 2;

        float offsetX = 0;
        float offsetY = 0;
        float offsetZ = 0;

        float centerX = 0;
        float centerY = 0;
        float centerZ = 0;

        public float lightX = 1;
        public float lightY = 1;
        public float lightZ = 1;
        SoulsFormats.FLVER.Vertex targetV = null;
        VertexInfo targetVinfo = null;
        ContextMenu cm = new ContextMenu();
        bool rightClickSilence = false;
        Form f;
        Texture2D testTexture;
        Dictionary<string, Texture2D> textureMap = new Dictionary<string, Texture2D>();
        private static GCHandle handle;
        public Mono3D()
        {
            Window.Title = "FLVER Viewer by Forsakensilver, press F to refresh, press F1 F2 F3 F4 F5 F9: Change render mode Right click: check vertex info B: Toggle bone display M: Dummy display";
            Window.AllowUserResizing = true;
            this.IsMouseVisible = true;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // string path = @"data\img\27.png";
            //test = Content.Load<Texture2D>(@"data\img\27.png");
            f =  (Form)Form.FromHandle(Window.Handle);

   
            cm.MenuItems.Add("Cancel");

            cm.MenuItems.Add("Check Vertex", new EventHandler(delegate (Object o, EventArgs a)
            {
                displayVerticesInfo();
            })
            );


            cm.MenuItems.Add("Edit Vertex", new EventHandler(delegate (Object o, EventArgs a)
            {
                editVerticesInfo();
            })
            );

            MenuItem mi0 = new MenuItem();
            mi0.Text = "Delete Selected Vertex's Faceset";
           // mi0.Shortcut = Shortcut.Alt1;
          //  mi0.ShowShortcut = true;
            mi0.Click += new EventHandler(delegate (Object o, EventArgs a)
            {
                deleteVertex();
                // editVerticesInfo();
                //    MessageBox.Show(targetV);
            });
            cm.MenuItems.Add(mi0);

          /*  cm.MenuItems.Add("Delete Vertex (related faceset)", new EventHandler(delegate (Object o, EventArgs a)
            {
                deleteVertex();
                // editVerticesInfo();
                //    MessageBox.Show(targetV);

            }));*/


            cm.MenuItems.Add("Delete Vertices Above", new EventHandler(delegate (Object o, EventArgs a)
            {
                deleteVertexAbove();
               // editVerticesInfo();
               //    MessageBox.Show(targetV);
           }));

            MenuItem mi = new MenuItem();
            mi.Text = "Delete Vertices Below";
        //    mi.Shortcut = Shortcut.CtrlD;
         //   mi.ShowShortcut = true;
            mi.Click += new EventHandler(delegate (Object o, EventArgs a)
            {
                deleteVertexBelow();
                // editVerticesInfo();
                //    MessageBox.Show(targetV);

            });
            cm.MenuItems.Add(mi);
            /*  cm.MenuItems.Add("Delete Vertices Below", new EventHandler(delegate (Object o, EventArgs a)
              {
                  deleteVertexBelow();
                  // editVerticesInfo();
                  //    MessageBox.Show(targetV);

              }));
              */

            cm.MenuItems.Add("Select Bone", new EventHandler(delegate (Object o, EventArgs a) {
                selectBone();
            }));

            f.ContextMenu = cm;
           
            f.MouseDown += new MouseEventHandler(this.pictureBox1_MouseDown);
            f.MouseUp += new MouseEventHandler(this.pictureBox1_MouseUp);
        }



        private void deleteVertexBelow()
        {
            SoulsFormats.FLVER.Mesh m = Program.targetFlver.Meshes[targetVinfo.meshIndex];
            uint index = targetVinfo.vertexIndex;
            float yValue = targetV.Positions[0].Y;
            for (int i = 0; i < m.Vertices.Count; i++)
            {
                if (m.Vertices[i].Positions[0].Y < yValue)
                {

                    deleteMeshVertexFaceset(m, (uint)i);
                    m.Vertices[i].Positions[0] = new System.Numerics.Vector3(0, 0, 0);
                }
            }

            Program.updateVertices();
        }

        private void deleteVertexAbove()
        {
            SoulsFormats.FLVER.Mesh m = Program.targetFlver.Meshes[targetVinfo.meshIndex];
            uint index = targetVinfo.vertexIndex;
            float yValue = targetV.Positions[0].Y;
            for (int i = 0;i < m.Vertices.Count;i++) 
            {
                if (m.Vertices[i].Positions[0].Y > yValue) 
                {

                    deleteMeshVertexFaceset(m, (uint)i);
                    m.Vertices[i].Positions[0] = new System.Numerics.Vector3(0,0,0);
                }
            }

            Program.updateVertices();
        }
        private void deleteMeshVertexFaceset(SoulsFormats.FLVER.Mesh m, uint index)
        {
            foreach (var fs in m.FaceSets)
            {
                for (uint i = 0; i + 2 < fs.Vertices.Length; i += 3)
                {
                    if (fs.Vertices[i] == index || fs.Vertices[i + 1] == index || fs.Vertices[i + 2] == index)
                    {
                        fs.Vertices[i] = index;
                        fs.Vertices[i + 1] = index;
                        fs.Vertices[i + 2] = index;
                    }
                }
            }
        }
        private void deleteVertex()
        {
            SoulsFormats.FLVER.Mesh m = Program.targetFlver.Meshes[targetVinfo.meshIndex];
            uint index = targetVinfo.vertexIndex;
            deleteMeshVertexFaceset(m,index);
       
            targetV.Positions[0] = new System.Numerics.Vector3(0,0,0);

            Program.updateVertices();
        }

        private static float dotProduct(Vector3 a, Vector3 b) {
            float x1 = a.X;
            float y1 = a.Y;
            float z1 = a.Z;
            float x2 = b.X;
            float y2 = b.Y;
            float z2 = b.Z;
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    {
                        f.ContextMenu = null;
                        prevMState = Mouse.GetState();

                        checkVerticesSilent();

                        f.ContextMenu = null;
                        //f.ContextMenu.Show();
                        if (prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) && !rightClickSilence) {
                            rightClickSilence = true;
                            System.Windows.MessageBox.Show("Ctrl + Right Click pressed. Switch To Right Click Slience Mode.");
                        }
                        else if (prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) && rightClickSilence) {
                            rightClickSilence = false;
                            System.Windows.MessageBox.Show("Ctrl + Right Click pressed. Switch To Right Click Non-Slience Mode.");
                        }
                    }
                    break;
                case MouseButtons.Left: {
                        if (prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt)) {
                            prevMState = Mouse.GetState();
                            Ray r = GetMouseRay(new Vector2(prevMState.Position.X, prevMState.Position.Y), GraphicsDevice.Viewport, effect);
                            r.Position = new Vector3(r.Position.X, r.Position.Z, r.Position.Y);
                            r.Direction = new Vector3(r.Direction.X, r.Direction.Z, r.Direction.Y);

                            Vector3D x1 = new Vector3D(r.Position);
                            Vector3D x2 = new Vector3D(r.Position + r.Direction);

                            float ptDistance = 0.02f;
                            foreach (SoulsFormats.FLVER.Vertex v in Program.vertices) {
                                if (v.Positions[0] == null) { continue; }
                                float dis = Vector3D.calculateDistanceFromLine(new Vector3D(v.Positions[0]), x1, x2);
                                if (ptDistance >= dis) {
                                    //TODO: smarter painting
                                    //Need some way to not paint the verts obscured by other faces
                                    if (v.Normals != null && v.Normals.Count > 0) {
                                        //Only paint front faces to help avoid bleedthrough
                                        Vector3 normal = new Vector3(v.Normals[0].X, v.Normals[0].Y, v.Normals[0].Z);

                                        if (dotProduct(Vector3.Normalize(normal), Vector3.Normalize(r.Direction)) < 0) {
                                            v.BoneIndices[0] = selectedBone;
                                            v.BoneWeights[0] = weightToPaint;
                                        }
                                    }
                                    else {
                                        // If we have no normals just paint
                                        v.BoneIndices[0] = selectedBone;
                                        v.BoneWeights[0] = weightToPaint;
                                    }
                                }

                            }

                            Program.updateVertices();
                        }
                    }
                    break;
            }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    {
                        if ( !rightClickSilence)
                        {
                            f.ContextMenu = cm;
                               f.ContextMenu.Show(f, new System.Drawing.Point(e.X + 1, e.Y + 1));//places the menu at the pointer position
                        }
                        else if (prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && rightClickSilence)
                        {
                            deleteVertex();
                           // System.Windows.MessageBox.Show("Ctrl + Right Click pressed. Switch To Right Click Slience Mode.");
                        }

                    }
                    break;
            }

        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            floorVerts = new VertexPositionTexture[6];
            floorVerts[0].Position = new Vector3(-20, -20, 0);
            floorVerts[1].Position = new Vector3(-20, 20, 0);
            floorVerts[2].Position = new Vector3(20, -20, 0);
            floorVerts[3].Position = floorVerts[1].Position;
            floorVerts[4].Position = new Vector3(20, 20, 0);
            floorVerts[5].Position = floorVerts[2].Position;

            effect = new BasicEffect(graphics.GraphicsDevice);
            effect.VertexColorEnabled = true;
            base.Initialize();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            testTexture = null;
            if (!Program.loadTexture) { return; }
            //decrypt tpf file;
            string tpfFile = Program.orgFileName.Substring(0,Program.orgFileName.Length - 5) + "tpf";

            try {

                if (Program.targetTPF != null)
                {
                    foreach (var t in Program.targetTPF.Textures)
                    {
                        textureMap.Add(t.Name, getTextureFromBitmap(readDdsStreamToBitmap(new MemoryStream(t.Bytes)), this.GraphicsDevice));
                        //   System.Windows.MessageBox.Show("Added:" + t.Name);
                    }
                }
                else
                 if (File.Exists(tpfFile))
                {
                    var tpf = SoulsFormats.TPF.Read(tpfFile);
                    foreach (var t in tpf.Textures)
                    {
                        textureMap.Add(t.Name, getTextureFromBitmap(readDdsStreamToBitmap(new MemoryStream(t.Bytes)), this.GraphicsDevice));
                        //   System.Windows.MessageBox.Show("Added:" + t.Name);
                    }


                }



            } catch (Exception e) 
            {
            
            }


            using (var stream = TitleContainer.OpenStream("singleColor.png"))
            {
                testTexture = Texture2D.FromStream(this.GraphicsDevice, stream);
            }
            //  testTexture = getTextureFromBitmap(readDdsFileToBitmap("EliteKnight.dds"),this.GraphicsDevice);


            /*  string path = @"data\img\27.png";

              System.Drawing.Bitmap btt = new System.Drawing.Bitmap(path);
              test = Texture2D.FromStream(this.GraphicsDevice, File.OpenRead(path));
              test = getTextureFromBitmap(btt, this.GraphicsDevice);*/
            // TODO: use this.Content to load your game content here
        }

        //Read dds file to bitmap
        System.Drawing.Bitmap readDdsFileToBitmap(string f)
        {

            Pfim.IImage image = Pfim.Pfim.FromFile(f);
            PixelFormat format;

            switch (image.Format)
            {
                case Pfim.ImageFormat.Rgb24:
                    format = PixelFormat.Format24bppRgb;
                    break;

                case Pfim.ImageFormat.Rgba32:
                    format = PixelFormat.Format32bppArgb;
                    break;

                case Pfim.ImageFormat.R5g5b5:
                    format = PixelFormat.Format16bppRgb555;
                    break;

                case Pfim.ImageFormat.R5g6b5:
                    format = PixelFormat.Format16bppRgb565;
                    break;


                case Pfim.ImageFormat.R5g5b5a1:
                    format = PixelFormat.Format16bppArgb1555;
                    break;



                case Pfim.ImageFormat.Rgb8:
                    format = PixelFormat.Format8bppIndexed;
                    break;



                default:
                   /* var msg = $"{image.Format} is not recognized for Bitmap on Windows Forms. " +

                               "You'd need to write a conversion function to convert the data to known format";

                    var caption = "Unrecognized format";

                    MessageBox.Show(msg, caption, MessageBoxButtons.OK);
                    */
                    return null;

            }



            if (handle.IsAllocated)
            {
                handle.Free();
            }

           handle = System.Runtime.InteropServices.GCHandle.Alloc(image.Data, System.Runtime.InteropServices.GCHandleType.Pinned);

            var ptr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);

            var bitmap = new System.Drawing.Bitmap(image.Width, image.Height, image.Stride, format, ptr);


            return bitmap;

        }


        System.Drawing.Bitmap readDdsStreamToBitmap(Stream  f)
        {

            Pfim.IImage image = Pfim.Pfim.FromStream(f);
            PixelFormat format;

            switch (image.Format)
            {
                case Pfim.ImageFormat.Rgb24:
                    format = PixelFormat.Format24bppRgb;
                    break;

                case Pfim.ImageFormat.Rgba32:
                    format = PixelFormat.Format32bppArgb;
                    break;

                case Pfim.ImageFormat.R5g5b5:
                    format = PixelFormat.Format16bppRgb555;
                    break;

                case Pfim.ImageFormat.R5g6b5:
                    format = PixelFormat.Format16bppRgb565;
                    break;


                case Pfim.ImageFormat.R5g5b5a1:
                    format = PixelFormat.Format16bppArgb1555;
                    break;



                case Pfim.ImageFormat.Rgb8:
                    format = PixelFormat.Format8bppIndexed;
                    break;



                default:
                    /* var msg = $"{image.Format} is not recognized for Bitmap on Windows Forms. " +

                                "You'd need to write a conversion function to convert the data to known format";

                     var caption = "Unrecognized format";

                     MessageBox.Show(msg, caption, MessageBoxButtons.OK);
                     */
                    return null;

            }



            if (handle.IsAllocated)
            {
                handle.Free();
            }

            handle = System.Runtime.InteropServices.GCHandle.Alloc(image.Data, System.Runtime.InteropServices.GCHandleType.Pinned);

            var ptr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);

            var bitmap = new System.Drawing.Bitmap(image.Width, image.Height, image.Stride, format, ptr);


            return bitmap;

        }

        //Refer to the code at http://florianblock.blogspot.com/2008/06/copying-dynamically-created-bitmap-to.html
        //Also refer to https://gamedev.stackexchange.com/questions/6440/bitmap-to-texture2d-problem-with-colors
        //Modied by Alan Zhang
        public static Texture2D getTextureFromBitmap(System.Drawing.Bitmap b, GraphicsDevice graphicsDevice)
        {
            Texture2D tx = null;
            using (MemoryStream s = new MemoryStream())
            {
                b.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                s.Seek(0, SeekOrigin.Begin);
                tx = Texture2D.FromStream(graphicsDevice, s);
            }
            return tx;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 


        protected void selectBone()
        {
            Form fn = new Form();
            fn.Size = new System.Drawing.Size(350, 650);

            Label label1 = new Label();
            label1.Text = "Selected Bone";
            label1.Location = new System.Drawing.Point(10, 50);

            NumericUpDown bone = new NumericUpDown();
            bone.Location = new System.Drawing.Point(10, 100);
            bone.Value = selectedBone;

            Label label2 = new Label();
            label2.Text = "Weight to Paint";
            label2.Location = new System.Drawing.Point(10, 150);

            TextBox tbp = new TextBox();
            tbp.Size = new System.Drawing.Size(300, 40);
            tbp.Location = new System.Drawing.Point(10, 200);
            tbp.Text = Convert.ToString(weightToPaint);

            Button bn = new Button();
            bn.Size = new System.Drawing.Size(330, 35);
            bn.Location = new System.Drawing.Point(5, 560);
            bn.Text = "Select";
            bn.Click += (s, o) => {
                selectedBone = (int)bone.Value;
                weightToPaint = float.Parse(tbp.Text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                Program.updateVertices();
            };

            fn.Controls.Add(label1);
            fn.Controls.Add(bone);
            fn.Controls.Add(label2);
            fn.Controls.Add(tbp);
            fn.Controls.Add(bn);
            fn.Show();
        }


        protected void checkVerticesSilent()
        {
            Ray r = GetMouseRay(new Vector2(prevMState.Position.X, prevMState.Position.Y), GraphicsDevice.Viewport, effect);
            r.Position = new Vector3(r.Position.X, r.Position.Z, r.Position.Y);
            r.Direction = new Vector3(r.Direction.X, r.Direction.Z, r.Direction.Y);
            // Vector3D x1 =  new Vector3D(cameraX + offsetX, cameraY + offsetY, cameraZ + offsetZ);
            //Vector3D x2 = new Vector3D(centerX + offsetX, centerY + offsetY, centerZ + offsetZ);
            Vector3D x1 = new Vector3D(r.Position);
            Vector3D x2 = new Vector3D(r.Position + r.Direction);
            //Program.useCheckingPoint = true;
            // Program.checkingPoint = new System.Numerics.Vector3(x2.X,x2.Z,x2.Y);
            // Program.updateVertices();
            Vector3D miniPoint = new Vector3D();
            float ptDistance = float.MaxValue;
            targetV = null;
            targetVinfo = null;

            for(int i = 0; i < Program.vertices.Count;i++)
          //  foreach (SoulsFormats.FLVER.Vertex v in Program.vertices)
            {
                SoulsFormats.FLVER.Vertex v = Program.vertices[i];
                if (v.Positions[0] == null) { continue; }
                float dis = Vector3D.calculateDistanceFromLine(new Vector3D(v.Positions[0]), x1, x2);
                if (ptDistance > dis)
                {

                    miniPoint = new Vector3D(v.Positions[0]);
                    ptDistance = dis;
                    targetV = v;
                    targetVinfo = Program.verticesInfo[i];
                }

            }

            if (Program.setVertexPos)
            {
                targetV.Positions[0] = new Vector3D(Program.setVertexX, Program.setVertexY, Program.setVertexZ).toNumV3();
            }

            Program.useCheckingPoint = true;
            Program.checkingPoint = new System.Numerics.Vector3(miniPoint.X, miniPoint.Y, miniPoint.Z);

            if (targetV.Normals != null && targetV.Normals.Count > 0)
            {
                Program.checkingPointNormal = new System.Numerics.Vector3(targetV.Normals[0].X, targetV.Normals[0].Y, targetV.Normals[0].Z);
            }
            else
            {
                Program.checkingPointNormal = new System.Numerics.Vector3(0, 0, 0);
            }

            Program.updateVertices();

         


        }
        protected void displayVerticesInfo()
        {

            if (targetV != null)
            {
                string text = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV));
                int l = text.Length / 2;
                System.Windows.Forms.MessageBox.Show("Parent mesh index:" + targetVinfo.meshIndex + "\nVertex index:" + targetVinfo.vertexIndex  + "\n"+ text.Substring(0, l), "Vertex info1:");
                System.Windows.Forms.MessageBox.Show(text.Substring(l, text.Length - l), "Vertex info2:");

            }
        }

        protected void editVerticesInfo()
        {

            if (targetV != null)
            {
                //string text = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV));
                //int l = text.Length / 2;
                Form fn = new Form();
                fn.Size = new System.Drawing.Size(350,650);

                string positions = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV.Positions));
                TextBox tbp = new TextBox();
                tbp.Size = new System.Drawing.Size(330,183);
                tbp.Location = new System.Drawing.Point(5, 10);
                tbp.Multiline = true;
                tbp.Text = positions;

                string indices = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV.BoneIndices));
                TextBox tbi = new TextBox();
                tbi.Size = new System.Drawing.Size(330, 183);
                tbi.Location = new System.Drawing.Point(5, 193);
                tbi.Multiline = true;
                tbi.Text = indices;

                string weights = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV.BoneWeights));
                TextBox tbw = new TextBox();
                tbw.Size = new System.Drawing.Size(330, 183);
                tbw.Location = new System.Drawing.Point(5, 386);
                tbw.Multiline = true;
                tbw.Text = weights;


                Button bn = new Button();
                bn.Size = new System.Drawing.Size(330, 35);
                bn.Location = new System.Drawing.Point(5, 560);
                bn.Text = "Modify";
                bn.Click += (s, o) => {
                    List<System.Numerics.Vector3> vp = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<System.Numerics.Vector3>>(tbp.Text);
                    int[] vi = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<int>>(tbi.Text).ToArray();
                    float[] vw = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<float>>(tbw.Text).ToArray();
                    targetV.Positions = vp;
                    targetV.BoneWeights = vw;
                    targetV.BoneIndices = vi;
                    Program.updateVertices();
                };


                fn.Controls.Add(tbp);
                fn.Controls.Add(tbi);
                fn.Controls.Add(tbw);
                fn.Controls.Add(bn);
                fn.Show();

            }
        }


        protected void checkVertices()
        {
            Ray r = GetMouseRay(new Vector2(prevMState.Position.X, prevMState.Position.Y), GraphicsDevice.Viewport, effect);
            r.Position = new Vector3(r.Position.X, r.Position.Z, r.Position.Y);
            r.Direction = new Vector3(r.Direction.X, r.Direction.Z, r.Direction.Y);
            // Vector3D x1 =  new Vector3D(cameraX + offsetX, cameraY + offsetY, cameraZ + offsetZ);
            //Vector3D x2 = new Vector3D(centerX + offsetX, centerY + offsetY, centerZ + offsetZ);
            Vector3D x1 = new Vector3D(r.Position);
            Vector3D x2 = new Vector3D(r.Position + r.Direction);
            //Program.useCheckingPoint = true;
            // Program.checkingPoint = new System.Numerics.Vector3(x2.X,x2.Z,x2.Y);
            // Program.updateVertices();
            Vector3D miniPoint = new Vector3D();
            float ptDistance = float.MaxValue;
            targetV = null;
            foreach (SoulsFormats.FLVER.Vertex v in Program.vertices)
            {
                if (v.Positions[0] == null) { continue; }
                float dis = Vector3D.calculateDistanceFromLine(new Vector3D(v.Positions[0]), x1, x2);
                if (ptDistance > dis)
                {

                    miniPoint = new Vector3D(v.Positions[0]);
                    ptDistance = dis;
                    targetV = v;
                }

            }

            if (Program.setVertexPos)
            {
                targetV.Positions[0] = new Vector3D(Program.setVertexX, Program.setVertexY, Program.setVertexZ).toNumV3();
            }

            Program.useCheckingPoint = true;
            Program.checkingPoint = new System.Numerics.Vector3(miniPoint.X, miniPoint.Y, miniPoint.Z);

            if (targetV.Normals != null && targetV.Normals.Count > 0)
            {
                Program.checkingPointNormal = new System.Numerics.Vector3(targetV.Normals[0].X, targetV.Normals[0].Y, targetV.Normals[0].Z);
            }
            else
            {
                Program.checkingPointNormal = new System.Numerics.Vector3(0, 0, 0);
            }

            Program.updateVertices();

            if (targetV != null)
            {
                string text = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV));
                int l = text.Length / 2;
                System.Windows.Forms.MessageBox.Show(text.Substring(0, l), "Vertex info1:");
                System.Windows.Forms.MessageBox.Show(text.Substring(l, text.Length - l), "Vertex info2:");

            }


        }


        protected override void Update(GameTime gameTime)
        {

            KeyboardState state = Keyboard.GetState();
            MouseState mState = Mouse.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Application.Exit();

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;



            //Marine_Yes00.mp3

           

            if (mState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && !Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && IsActive)
            {
                float mdx = mState.X - prevMState.X;
                float mdy = mState.Y - prevMState.Y;
                {
                    System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);
                    System.Numerics.Vector3 p2 = Program.RotatePoint(p, 0, 0, -mdx * 0.01f);
                    cameraX = p2.X;
                    cameraY = p2.Y;
                    cameraZ = p2.Z;
                }
                {
                    System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);

                    float nX = cameraY;
                    float nY = -cameraX;
                 

                    System.Numerics.Vector3 p2 = Program.RotateLine(p, new System.Numerics.Vector3(0, 0, 0),
                        new System.Numerics.Vector3(nX, nY, 0), mdy * 0.01f);


                    cameraX = p2.X;
                    cameraY = p2.Y;
                    cameraZ = p2.Z;

                }

                

            }

            if (mState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && IsActive)
            {
                float mdx = mState.X - prevMState.X;
                float mdy = mState.Y - prevMState.Y;

               // offsetZ += mdy * 3 * delta;

                Vector3D upV = new Vector3D(0, 0, 1);
                Vector3D forwardV = new Vector3D(cameraX, cameraY, cameraZ);
                Vector3D rightV = Vector3D.crossPorduct(upV, forwardV).normalize();
                Vector3D camUpV = Vector3D.crossPorduct(forwardV, rightV).normalize();

                Vector3D offsetV = new Vector3D(offsetX,offsetY,offsetZ);
                offsetV = offsetV - new Vector3D(rightV.X * mdx * 0.01f, rightV.Y * mdx * 0.01f, rightV.Z * mdx * 0.01f);
                offsetV = offsetV + new Vector3D(camUpV.X * mdy * 0.01f, camUpV.Y * mdy * 0.01f, camUpV.Z * mdy * 0.01f);

                offsetX = offsetV.X;
                offsetY = offsetV.Y;
                offsetZ = offsetV.Z;
                //offsetX -= mdx* 1 * delta * rightV.X;
                //offsetY -= mdx * 1 * delta * rightV.Y;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1))
            {
                renderMode = RenderMode.Line;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F2))
            {
                renderMode = RenderMode.Triangle;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F3))
            {
                renderMode = RenderMode.Both;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F4))
            {
                renderMode = RenderMode.BothNoTex;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                renderMode = RenderMode.TexOnly;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F9))
            {
                renderMode = RenderMode.Weights;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F6) && !prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F6))
            {
                flatShading = !flatShading;
                Program.updateVertices();
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B) && !prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B))
            {
                Program.boneDisplay = !Program.boneDisplay;
                Program.updateVertices();
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M) && !prevState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M))
            {
                Program.dummyDisplay = !Program.dummyDisplay;
                Program.updateVertices();
            }

            //1.73 Added focus detect
            if (mState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && this.IsActive && false)
            {
                Ray r = GetMouseRay(new Vector2(mState.Position.X,mState.Position.Y), GraphicsDevice.Viewport, effect);
                r.Position = new Vector3(r.Position.X,r.Position.Z,r.Position.Y);
                r.Direction = new Vector3(r.Direction.X,r.Direction.Z,r.Direction.Y);
                // Vector3D x1 =  new Vector3D(cameraX + offsetX, cameraY + offsetY, cameraZ + offsetZ);
                //Vector3D x2 = new Vector3D(centerX + offsetX, centerY + offsetY, centerZ + offsetZ);
                Vector3D x1 = new Vector3D(r.Position);
                Vector3D x2 = new Vector3D(r.Position + r.Direction);
                //Program.useCheckingPoint = true;
               // Program.checkingPoint = new System.Numerics.Vector3(x2.X,x2.Z,x2.Y);
               // Program.updateVertices();
                Vector3D miniPoint = new Vector3D();
                float ptDistance = float.MaxValue;
                SoulsFormats.FLVER.Vertex targetV = null;
                foreach (SoulsFormats.FLVER.Vertex v in Program.vertices)
                {
                    if (v.Positions[0] == null) { continue; }
                    float dis = Vector3D.calculateDistanceFromLine(new Vector3D(v.Positions[0]), x1, x2);
                    if (ptDistance > dis)
                    {

                        miniPoint = new Vector3D(v.Positions[0]);
                        ptDistance = dis;
                        targetV = v;
                    }

                }
                
                if (Program.setVertexPos)
                {
                    targetV.Positions[0] = new Vector3D(Program.setVertexX, Program.setVertexY, Program.setVertexZ).toNumV3();
                }

                Program.useCheckingPoint = true;
                Program.checkingPoint = new System.Numerics.Vector3(miniPoint.X, miniPoint.Y, miniPoint.Z);

                if (targetV.Normals != null && targetV.Normals.Count > 0)
                {
                    Program.checkingPointNormal = new System.Numerics.Vector3(targetV.Normals[0].X, targetV.Normals[0].Y, targetV.Normals[0].Z);
                }
                else {
                    Program.checkingPointNormal = new System.Numerics.Vector3(0, 0, 0);
                }
                
                Program.updateVertices();

                if (targetV != null) {
                    string text = Program.FormatOutput(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(targetV));
                    int l = text.Length / 2;
                    System.Windows.Forms.MessageBox.Show(  text.Substring(0,l)  ,"Vertex info1:");
                    System.Windows.Forms.MessageBox.Show(text.Substring(l,text.Length- l), "Vertex info2:");

                }
            }

            if (mState.ScrollWheelValue - prevMState.ScrollWheelValue > 0)
            {
                //mouseY -= (50 * delta);
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);



                cameraX = p.X - 0.5f * (float)(p.X / p.Length());
                cameraY = p.Y - 0.5f * (float)(p.Y / p.Length());
                cameraZ = p.Z - 0.5f * (float)(p.Z / p.Length());
            }

            if (mState.ScrollWheelValue - prevMState.ScrollWheelValue < 0)
            {
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);


                cameraX = p.X + 0.5f * (float)(p.X / p.Length());
                cameraY = p.Y + 0.5f * (float)(p.Y / p.Length());
                cameraZ = p.Z + 0.5f * (float)(p.Z / p.Length());
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX,cameraY,cameraZ);
                System.Numerics.Vector3 p2 = Program.RotatePoint(p, 0, 0, 5 * delta);
                cameraX = p2.X;
                cameraY = p2.Y;
                cameraZ = p2.Z;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);
                System.Numerics.Vector3 p2 = Program.RotatePoint(p, 0, 0, -5 * delta);
                cameraX = p2.X;
                cameraY = p2.Y;
                cameraZ = p2.Z;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                //mouseY -= (50 * delta);
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);


                System.Numerics.Vector3 p2 = Program.RotateLine(p, new System.Numerics.Vector3(0, 0, 0),
                    new System.Numerics.Vector3(cameraY, -cameraX, 0), 3 * delta);
                cameraX = p2.X;
                cameraY = p2.Y;
                cameraZ = p2.Z;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);


                System.Numerics.Vector3 p2 = Program.RotateLine(p, new System.Numerics.Vector3(0, 0, 0),
                    new System.Numerics.Vector3(cameraY, -cameraX, 0), -3 * delta);
                cameraX = p2.X;
                cameraY = p2.Y;
                cameraZ = p2.Z;
            }


            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemComma))
            {
                //mouseY -= (50 * delta);
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);


                
                cameraX = p.X - 3 *delta * (float)(p.X / p.Length());
                cameraY = p.Y - 3 * delta * (float)(p.Y / p.Length());
                cameraZ = p.Z-  3 * delta * (float)(p.Z / p.Length());
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemPeriod))
            {
                System.Numerics.Vector3 p = new System.Numerics.Vector3(cameraX, cameraY, cameraZ);


                cameraX = p.X + 3 * delta * (float)(p.X / p.Length());
                cameraY = p.Y + 3 * delta * (float)(p.Y / p.Length());
                cameraZ = p.Z + 3 * delta * (float)(p.Z / p.Length());
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad8))
            {
                offsetZ += 3 * delta;

            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad2))
            {

                offsetZ -= 3 * delta; ;
            }

            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad4))
            {
                Vector3D upV = new Vector3D(0,0,1);
                Vector3D forwardV = new Vector3D(cameraX,cameraY,cameraZ);
                Vector3D rightV = Vector3D.crossPorduct(upV,forwardV).normalize();

                offsetX -= 3 * delta * rightV.X;
                offsetY -= 3 * delta * rightV.Y;
                //offsetZ -= 3 * delta; ;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad6))
            {
                Vector3D upV = new Vector3D(0, 0, 1);
                Vector3D forwardV = new Vector3D(cameraX, cameraY, cameraZ);
                Vector3D rightV = Vector3D.crossPorduct(upV, forwardV).normalize();

                offsetX += 3 * delta * rightV.X;
                offsetY += 3 * delta * rightV.Y;
                //offsetZ -= 3 * delta; ;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad5))
            {

                Vector3D forwardV = new Vector3D(cameraX, cameraY, cameraZ).normalize();
 

                offsetX -= 3 * delta * forwardV.X;
                offsetY -= 3 * delta * forwardV.Y;
                offsetZ -= 3 * delta * forwardV.Z;
                //offsetZ -= 3 * delta; ;
            }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad0))
            {
                Vector3D forwardV = new Vector3D(cameraX, cameraY, cameraZ).normalize();

                offsetX += 3 * delta * forwardV.X;
                offsetY += 3 * delta * forwardV.Y;
                offsetZ += 3 * delta * forwardV.Z;
                //offsetZ -= 3 * delta; ;
            }

            //new Vector3(cameraX + offsetX, cameraY + offsetY, cameraZ + offsetZ)



           /* if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1)) { Program.rotOrder = RotationOrder.XYZ; Program.updateVertices(); }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2)) { Program.rotOrder = RotationOrder.XZY; Program.updateVertices(); }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3)) { Program.rotOrder = RotationOrder.YXZ; Program.updateVertices(); }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4)) { Program.rotOrder = RotationOrder.YZX; Program.updateVertices(); }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D5)) { Program.rotOrder = RotationOrder.ZXY; Program.updateVertices(); }
            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D6)) { Program.rotOrder = RotationOrder.ZYX; Program.updateVertices(); }*/



            if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F))
            {

                Program.updateVertices();
            }
            //mouseX = Mouse.GetState().Position.X;
            //mouseY = Mouse.GetState().Position.Y;
            // TODO: Add your update logic here

            prevState = state;
            prevMState = mState;
            base.Update(gameTime);
        }

        private Vector3 RotatePoint()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            // TODO: Add your drawing code here
            // Rectangle screenRectangle = new Rectangle((int)mouseX, (int)mouseY, 50, 50);
            //  spriteBatch.Draw(test, screenRectangle, Color.White);
            DrawGround();
            //effect.EnableDefaultLighting();
            /*var vertices = new VertexPositionColor[4];
             vertices[0].Position = new Vector3(100, 100, 0);
             vertices[0].Color = Color.Black;
             vertices[1].Position = new Vector3(200, 100, 0);
             vertices[1].Color = Color.Red;
             vertices[2].Position = new Vector3(200, 200, 0);
             vertices[2].Color = Color.Black;
             vertices[3].Position = new Vector3(100, 200, 0);
             vertices[3].Color = Color.Red;
             */
            /*if (renderMode == RenderMode.Triangle)
            {
                effect.LightingEnabled = true;
                effect.VertexColorEnabled = false;

            }
            else
            {
                effect.LightingEnabled = false;
                effect.VertexColorEnabled = true;
                
            }*/

            if (vertices.Length > 0 || triVertices.Length > 0)
            {
                if (renderMode == RenderMode.Line && vertices.Length > 0)
                {
                    GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);

                }
                else if (renderMode == RenderMode.Triangle && triVertices.Length > 0)
                {

                    graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triVertices, 0, triVertices.Length / 3);
                }
                else if (renderMode == RenderMode.Weights && triWeightVertices.Length > 0)
                {
                    graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triWeightVertices, 0, triWeightVertices.Length / 3);
                }
                else
                {

                    if (renderMode != RenderMode.TexOnly)
                        if (vertices.Length > 0) { GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, vertices, 0, vertices.Length / 2); }

                    if (renderMode == RenderMode.BothNoTex || Program.loadTexture == false)
                    {
                        if (triVertices.Length > 0) { graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triVertices, 0, triVertices.Length / 3); }
                    }
                    else {
                        foreach (var mi in meshInfos) 
                        {
                            if (textureMap.ContainsKey(mi.textureName))
                            {
                                effect.TextureEnabled = true;
                                effect.Texture = textureMap[mi.textureName];
                                //no texture found, don't draw.
                                //continue;
                            }
                            else {
                                effect.TextureEnabled = true;
                                effect.Texture = testTexture;
                            }
                           
                            foreach (var pass in effect.CurrentTechnique.Passes)
                            {
                                pass.Apply();

                                if (mi.triTextureVertices.Length > 0) { graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mi.triTextureVertices, 0, mi.triTextureVertices.Length / 3); }

                            }

                        }
                


                    }

               

                
                
                
                }
            }


            spriteBatch.End();
            base.Draw(gameTime);
        }

        void DrawGround()
        {
            // The assignment of effect.View and effect.Projection
            // are nearly identical to the code in the Model drawing code.
            // var cameraPosition = new Vector3(0 + mouseX, 40 + mouseY, 20);
            var cameraPosition = new Vector3(cameraX + offsetX, cameraY + offsetY, cameraZ + offsetZ);
            var cameraLookAtVector =new Vector3(centerX + offsetX,centerY + offsetY,centerZ + offsetZ);
            var cameraUpVector = Vector3.UnitZ;

            DepthStencilState depthBufferState = new DepthStencilState();
            depthBufferState.DepthBufferEnable = true;
            depthBufferState.DepthBufferFunction = CompareFunction.LessEqual;
            GraphicsDevice.DepthStencilState = depthBufferState;


            
            effect.View = Matrix.CreateLookAt(
                cameraPosition, cameraLookAtVector, cameraUpVector);
            effect.VertexColorEnabled = true;
            float aspectRatio =
                graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight;
            float fieldOfView = Microsoft.Xna.Framework.MathHelper.PiOver4;
            float nearClipPlane = 0.1f;
            float farClipPlane = 200;

            effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                fieldOfView, aspectRatio, nearClipPlane, farClipPlane);



            /* foreach (var pass in effect.CurrentTechnique.Passes)
             {
                 pass.Apply();

                 graphics.GraphicsDevice.DrawUserPrimitives(
                     // We’ll be rendering two trinalges
                     PrimitiveType.TriangleList,
                     // The array of verts that we want to render
                     floorVerts,
                     // The offset, which is 0 since we want to start 
                     // at the beginning of the floorVerts array
                     0,
                     // The number of triangles to draw
                     2);
             }*/


            VertexPositionColor[] lines;
            lines = new VertexPositionColor[6];

            lines[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);

            lines[1] = new VertexPositionColor(new Vector3(10, 0, 0), Color.Red);

            lines[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);

            lines[3] = new VertexPositionColor(new Vector3(0, 10, 0), Color.Blue);

            lines[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Yellow);

            lines[5] = new VertexPositionColor(new Vector3(0, 0, 10), Color.Yellow);
            effect.TextureEnabled = false;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)

            {
                pass.Apply();

                   graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lines, 0, 3);

            }


        }

        public static Ray GetMouseRay(Vector2 mousePosition, Viewport viewport, BasicEffect camera)
        {
            Vector3 nearPoint = new Vector3(mousePosition, 0);
            Vector3 farPoint = new Vector3(mousePosition, 1);

            nearPoint = viewport.Unproject(nearPoint, camera.Projection, camera.View, Matrix.Identity);
            farPoint = viewport.Unproject(farPoint, camera.Projection, camera.View, Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
        }

    }
}
