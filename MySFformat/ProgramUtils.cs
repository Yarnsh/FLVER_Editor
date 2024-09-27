﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.UI;
using System.Web.Script.Serialization;


using SoulsFormats;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using ObjLoader.Loader.Loaders;

using Assimp;
using System.Data;

namespace MySFformat
{
    static partial class Program
    {

        public static Microsoft.Xna.Framework.Vector3 toXnaV3(System.Numerics.Vector3 v)
        {

            return new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
        }

        public static Microsoft.Xna.Framework.Vector3 toXnaV3XZY(System.Numerics.Vector3 v)
        {

            return new Microsoft.Xna.Framework.Vector3(v.X, v.Z, v.Y);
        }



        public static Vector3 findBoneTrans(List<FLVER2.Bone> b, int index, Vector3 v = new Vector3())
        {
            /* if (bonePosList[index] != null)
             {
                 return bonePosList[index];
             }

             if (b[index].ParentIndex == -1)
             {
                 v += b[index].Translation;
                 return v;
             }
             Vector3 ans = findBoneTrans(b, b[index].ParentIndex, v);
             bonePosList[index] = ans;
             return ans;*/
            if (bonePosList[index] != null)
            {
                return bonePosList[index].toNumV3();
            }


            if (b[index].ParentIndex == -1)
            {

                bonePosList[index] = new Vector3D(b[index].Translation);
                return b[index].Translation;
            }

            Vector3 ans = b[index].Translation + findBoneTrans(b, b[index].ParentIndex);
            bonePosList[index] = new Vector3D(ans);


            return ans;



        }



        class VertexNormalList
        {
            public List<Vector3D> normals = new List<Vector3D>();
            public VertexNormalList()
            {
            }
            public Vector3D calculateAvgNormal()
            {
                Vector3D ans = new Vector3D();
                foreach (var n in normals)
                {
                    ans = ans + n;
                }
                return ans.normalize();
            }
            public void add(Vector3D a)
            {
                normals.Add(a);
            }

        }





        /// <summary>
        /// Deprecated, cannot solve tangent properly.
        /// </summary>
        static void importObj()
        {
            var openFileDialog2 = new OpenFileDialog();
            string res = "";
            if (openFileDialog2.ShowDialog() == DialogResult.No)
            {
                return;
            }
            res = openFileDialog2.FileName;
            var objLoaderFactory = new ObjLoaderFactory();
            MaterialStreamProvider msp = new MaterialStreamProvider();
            var openFileDialog3 = new OpenFileDialog();
            openFileDialog3.Title = "Choose MTL file:";
            if (openFileDialog3.ShowDialog() == DialogResult.No)
            {
                return;
            }

            msp.Open(openFileDialog3.FileName);
            var objLoader = objLoaderFactory.Create(msp);
            FileStream fileStream = new FileStream(res, FileMode.Open);
            LoadResult result = objLoader.Load(fileStream);



            // ObjLoader.Loader.Data.Elements.Face f = result.Groups[0].Faces[0];
            // ObjLoader.Loader.Data.Elements.FaceVertex[] fv =getVertices(f);

            // string groups = new JavaScriptSerializer().Serialize(fv);
            //string vertices = new JavaScriptSerializer().Serialize(result.Vertices);

            //MessageBox.Show(groups,"Group info");
            // MessageBox.Show(vertices, "V info");
            fileStream.Close();

            //Step 1 add a new buffer layout for my program:
            int layoutCount = targetFlver.BufferLayouts.Count;
            FLVER2.BufferLayout newBL = new FLVER2.BufferLayout();

            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Float3, FLVER.LayoutSemantic.Position, 0, 0));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4B, FLVER.LayoutSemantic.Normal, 0, 12));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4B, FLVER.LayoutSemantic.Tangent, 0, 16));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4B, FLVER.LayoutSemantic.Tangent, 1, 20));

            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4B, FLVER.LayoutSemantic.BoneIndices, 0, 24));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4C, FLVER.LayoutSemantic.BoneWeights, 0, 28));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.Byte4C, FLVER.LayoutSemantic.VertexColor, 1, 32));
            newBL.Add(new FLVER.LayoutMember(FLVER.LayoutType.UVPair, FLVER.LayoutSemantic.UV, 0, 36));

            targetFlver.BufferLayouts.Add(newBL);

            int materialCount = targetFlver.Materials.Count;




            FLVER2.Mesh mn = new FLVER2.Mesh();
            mn.MaterialIndex = 0;
            mn.BoneIndices = new List<int>();
            mn.BoneIndices.Add(0);
            mn.BoneIndices.Add(1);
            mn.BoundingBox.Max = new Vector3(1, 1, 1);
            mn.BoundingBox.Min = new Vector3(-1, -1, -1);
            mn.BoundingBox.Unk = new Vector3();
            //mn.Unk1 = 0;
            mn.DefaultBoneIndex = 0;
            mn.Dynamic = false;
            mn.VertexBuffers = new List<FLVER2.VertexBuffer>();
            mn.VertexBuffers.Add(new FLVER2.VertexBuffer(layoutCount));
            mn.Vertices = new List<FLVER.Vertex>();
            // mn.Vertices.Add(generateVertex(new Vector3(1,0,0),new Vector3(0,0,0),new Vector3(0,0,0),new Vector3(0,1,0),new Vector3(1,0,0)));
            //mn.Vertices.Add(generateVertex(new Vector3(0, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0)));
            //mn.Vertices.Add(generateVertex(new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0)));
            if (result.Groups.Count == 0)
            {
                MessageBox.Show("You imported nothing!");
                return;
            }
            MessageBox.Show("Vertice number:" + result.Vertices.Count + "Texture V number:" + result.Textures.Count + "Normal number:" + result.Normals.Count + "Face groups:" + result.Groups[0].Faces.Count);

            VertexNormalList[] vnlist = new VertexNormalList[result.Vertices.Count + 1];
            for (int i = 0; i < vnlist.Length; i++)
            {
                vnlist[i] = new VertexNormalList();
            }

            List<int> faceIndexs = new List<int>();
            uint[] textureIndexs = new uint[result.Vertices.Count + 1];
            foreach (var gr in result.Groups)
            {

                foreach (var faces in gr.Faces)
                {

                    var vList = getVertices(faces);
                    /*for (int i3 = 0; i3 < vList.Length - 2; i3++)
                    {
                        faceIndexs.Add((uint)(vList[i3].VertexIndex)-1);
                        faceIndexs.Add((uint)(vList[i3+1].VertexIndex)-1);
                        faceIndexs.Add((uint)(vList[i3+2].VertexIndex)-1);
                    }*/
                    if (vList.Length == 4)
                    {
                        faceIndexs.Add((int)(vList[0].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[2].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[1].VertexIndex) - 1);

                        //record normal to help calculate vertex normals
                        int helperI = 0;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 2;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 1;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);


                        faceIndexs.Add((int)(vList[2].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[0].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[3].VertexIndex) - 1);

                        helperI = 2;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 0;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex].Y, result.Normals[vList[helperI].NormalIndex].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 3;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex].X, result.Normals[vList[helperI].NormalIndex].Y, result.Normals[vList[helperI].NormalIndex].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                    }
                    else if (vList.Length == 3)
                    {

                        faceIndexs.Add((int)(vList[0].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[2].VertexIndex) - 1);
                        faceIndexs.Add((int)(vList[1].VertexIndex) - 1);


                        int helperI = 2;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 0;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);

                        helperI = 1;
                        vnlist[(uint)(vList[helperI].VertexIndex) - 1].add(new Vector3D(result.Normals[vList[helperI].NormalIndex - 1].X, result.Normals[vList[helperI].NormalIndex - 1].Y, result.Normals[vList[helperI].NormalIndex - 1].Z));
                        textureIndexs[(vList[helperI].VertexIndex) - 1] = ((uint)vList[helperI].TextureIndex - 1);
                    }
                }


            }
            //mn.FaceSets[0].Vertices = new uint [3]{0,1,2 };


            mn.FaceSets = new List<FLVER2.FaceSet>();
            //FLVER.Vertex myv = new FLVER.Vertex();
            //myv.Colors = new List<FLVER.Vertex.Color>();
            mn.FaceSets.Add(generateBasicFaceSet());
            mn.FaceSets[0].Indices = faceIndexs;



            //Set all the vertices.
            for (int iv = 0; iv < result.Vertices.Count; iv++)
            {
                var v = result.Vertices[iv];

                Vector3 uv1 = new Vector3();
                Vector3 uv2 = new Vector3();
                Vector3 normal = new Vector3(0, 1, 0);
                Vector3 tangent = new Vector3(1, 0, 0);
                if (result.Textures != null)
                {
                    if (iv < result.Textures.Count)
                    {

                        var vm = result.Textures[(int)textureIndexs[iv]];
                        uv1 = new Vector3(vm.X, vm.Y, 0);
                        uv2 = new Vector3(vm.X, vm.Y, 0);
                    }
                }
                normal = vnlist[iv].calculateAvgNormal().toNumV3();
                tangent = RotatePoint(normal, 0, (float)Math.PI / 2, 0);
                mn.Vertices.Add(generateVertex(new Vector3(v.X, v.Y, v.Z), uv1, uv2, normal, tangent));

            }
            FLVER2.Material matnew = new JavaScriptSerializer().Deserialize<FLVER2.Material>(new JavaScriptSerializer().Serialize(targetFlver.Materials[0]));
            matnew.Name = res.Substring(res.LastIndexOf('\\') + 1);
            targetFlver.Materials.Add(matnew);
            mn.MaterialIndex = materialCount;


            targetFlver.Meshes.Add(mn);
            MessageBox.Show("Added a custom mesh! PLease click modify to save it!");
            updateVertices();
            //mn.Vertices.Add();
        }






        static void printNodeStruct(Node n, int depth = 0, String parent = null)
        {

            if (n.ChildCount == 0)
            {
                string pred = "";
                for (int i = 0; i < depth; i++) { pred += "\t"; }
                if (!n.Name.Contains("$AssimpFbx$"))
                {
                    if (!boneParentList.ContainsKey(n.Name))
                        boneParentList.Add(n.Name, parent);
                    if (parent == null) { parent = ""; }
                    Console.Write(pred + parent + "->" + n.Name + "\n");

                }


            }
            else
            {
                string pred = "";
                for (int i = 0; i < depth; i++) { pred += "\t"; }
                string nextParent = parent;
                int increase = 0;
                if (!n.Name.Contains("$AssimpFbx$"))
                {
                    nextParent = n.Name;
                    if (!boneParentList.ContainsKey(n.Name))
                        boneParentList.Add(n.Name, parent);

                    if (parent == null) { parent = ""; }
                    increase = 1;
                    Console.Write(pred + parent + "->" + n.Name + "\n");
                }

                foreach (Node nn in n.Children)
                {

                    printNodeStruct(nn, depth + increase, nextParent);
                }

            }
        }

        static Vector3D getMyV3D(Assimp.Vector3D v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }

        static FLVER2.FaceSet generateBasicFaceSet()
        {
            FLVER2.FaceSet ans = new FLVER2.FaceSet();
            ans.CullBackfaces = true;
            ans.TriangleStrip = false;
            ans.Unk06 = 1;
            ans.Unk07 = 0;
            //ans.IndexSize = 16;

            return ans;

        }

        static FLVER.Vertex generateVertex(Vector3 pos, Vector3 uv1, Vector3 uv2, Vector3 normal, Vector3 tangets, int tangentW = -1)
        {
            FLVER.Vertex ans = new FLVER.Vertex();
            ans.Position = new Vector3();
            ans.Position = pos;
            ans.BoneIndices = new int[4] { 0, 0, 0, 0 };
            ans.BoneWeights = new float[4] { 1, 0, 0, 0 };
            ans.UVs = new List<Vector3>();
            ans.UVs.Add(uv1);
            ans.UVs.Add(uv2);
            ans.Normal = new Vector4();
            ans.Normal = new Vector4(normal.X, normal.Y, normal.Z, -1f);
            ans.Tangents = new List<Vector4>();
            ans.Tangents.Add(new Vector4(tangets.X, tangets.Y, tangets.Z, tangentW));
            ans.Tangents.Add(new Vector4(tangets.X, tangets.Y, tangets.Z, tangentW));
            ans.Colors = new List<FLVER.VertexColor>();
            ans.Colors.Add(new FLVER.VertexColor(255, 255, 255, 255));

            return ans;
        }


        static FLVER.Vertex generateVertex(Vector3 pos, Vector3 uv1, Vector3 uv2, Vector4 normal, Vector4 tangets, int tangentW = -1)
        {
            FLVER.Vertex ans = new FLVER.Vertex();
            ans.Position = new Vector3();
            ans.Position = pos;
            ans.BoneIndices = new int[4] { 0, 0, 0, 0 };
            ans.BoneWeights = new float[4] { 1, 0, 0, 0 };
            ans.UVs = new List<Vector3>();
            ans.UVs.Add(uv1);
            ans.UVs.Add(uv2);
            ans.Normal = new Vector4();
            ans.Normal = (new Vector4(normal.X, normal.Y, normal.Z, normal.W));
            ans.Tangents = new List<Vector4>();
            ans.Tangents.Add(new Vector4(tangets.X, tangets.Y, tangets.Z, tangets.W));
            ans.Tangents.Add(new Vector4(tangets.X, tangets.Y, tangets.Z, tangets.W));
            ans.Colors = new List<FLVER.VertexColor>();
            ans.Colors.Add(new FLVER.VertexColor(255, 255, 255, 255));

            return ans;
        }

        static FLVER.Vertex generateVertex2tan(Vector3 pos, Vector3 uv1, Vector3 uv2, Vector3 normal, Vector3 tangets, Vector3 tangets2, int tangentW = -1)
        {
            FLVER.Vertex ans = new FLVER.Vertex();
            ans.Position = new Vector3();
            ans.Position = pos;
            ans.BoneIndices = new int[4] { 0, 0, 0, 0 };
            ans.BoneWeights = new float[4] { 1, 0, 0, 0 };
            ans.UVs = new List<Vector3>();
            ans.UVs.Add(uv1);
            ans.UVs.Add(uv2);
            ans.Normal = new Vector4();
            ans.Normal = (new Vector4(normal.X, normal.Y, normal.Z, -1f));
            ans.Tangents = new List<Vector4>();
            ans.Tangents.Add(new Vector4(tangets.X, tangets.Y, tangets.Z, tangentW));
            ans.Tangents.Add(new Vector4(tangets2.X, tangets2.Y, tangets2.Z, tangentW));
            ans.Colors = new List<FLVER.VertexColor>();
            ans.Colors.Add(new FLVER.VertexColor(255, 255, 255, 255));

            return ans;
        }


        /*************** Basic Tools section *****************/

        public static Vector3 RotatePoint(Vector3 p, float pitch, float roll, float yaw)
        {

            Vector3 ans = new Vector3(0, 0, 0);


            var cosa = Math.Cos(yaw);
            var sina = Math.Sin(yaw);

            var cosb = Math.Cos(pitch);
            var sinb = Math.Sin(pitch);

            var cosc = Math.Cos(roll);
            var sinc = Math.Sin(roll);

            var Axx = cosa * cosb;
            var Axy = cosa * sinb * sinc - sina * cosc;
            var Axz = cosa * sinb * cosc + sina * sinc;

            var Ayx = sina * cosb;
            var Ayy = sina * sinb * sinc + cosa * cosc;
            var Ayz = sina * sinb * cosc - cosa * sinc;

            var Azx = -sinb;
            var Azy = cosb * sinc;
            var Azz = cosb * cosc;

            var px = p.X;
            var py = p.Y;
            var pz = p.Z;

            ans.X = (float)(Axx * px + Axy * py + Axz * pz);
            ans.Y = (float)(Ayx * px + Ayy * py + Ayz * pz);
            ans.Z = (float)(Azx * px + Azy * py + Azz * pz);


            return ans;
        }
        public static Vector4 RotatePoint(Vector4 p, float pitch, float roll, float yaw)
        {

            Vector4 ans = new Vector4(0, 0, 0, p.W);


            var cosa = Math.Cos(yaw);
            var sina = Math.Sin(yaw);

            var cosb = Math.Cos(pitch);
            var sinb = Math.Sin(pitch);

            var cosc = Math.Cos(roll);
            var sinc = Math.Sin(roll);

            var Axx = cosa * cosb;
            var Axy = cosa * sinb * sinc - sina * cosc;
            var Axz = cosa * sinb * cosc + sina * sinc;

            var Ayx = sina * cosb;
            var Ayy = sina * sinb * sinc + cosa * cosc;
            var Ayz = sina * sinb * cosc - cosa * sinc;

            var Azx = -sinb;
            var Azy = cosb * sinc;
            var Azz = cosb * cosc;

            var px = p.X;
            var py = p.Y;
            var pz = p.Z;

            ans.X = (float)(Axx * px + Axy * py + Axz * pz);
            ans.Y = (float)(Ayx * px + Ayy * py + Ayz * pz);
            ans.Z = (float)(Azx * px + Azy * py + Azz * pz);


            return ans;
        }

        public static Vector3 RotateLine(Vector3 p, Vector3 org, Vector3 direction, double theta)
        {
            double x = p.X;
            double y = p.Y;
            double z = p.Z;

            double a = org.X;
            double b = org.Y;
            double c = org.Z;



            double nu = direction.X / direction.Length();
            double nv = direction.Y / direction.Length();
            double nw = direction.Z / direction.Length();

            double[] rP = new double[3];

            rP[0] = (a * (nv * nv + nw * nw) - nu * (b * nv + c * nw - nu * x - nv * y - nw * z)) * (1 - Math.Cos(theta)) + x * Math.Cos(theta) + (-c * nv + b * nw - nw * y + nv * z) * Math.Sin(theta);
            rP[1] = (b * (nu * nu + nw * nw) - nv * (a * nu + c * nw - nu * x - nv * y - nw * z)) * (1 - Math.Cos(theta)) + y * Math.Cos(theta) + (c * nu - a * nw + nw * x - nu * z) * Math.Sin(theta);
            rP[2] = (c * (nu * nu + nv * nv) - nw * (a * nu + b * nv - nu * x - nv * y - nw * z)) * (1 - Math.Cos(theta)) + z * Math.Cos(theta) + (-b * nu + a * nv - nv * x + nu * y) * Math.Sin(theta);


            Vector3 ans = new Vector3((float)rP[0], (float)rP[1], (float)rP[2]);
            return ans;


        }

        public static Microsoft.Xna.Framework.Vector3 crossPorduct(Microsoft.Xna.Framework.Vector3 a, Microsoft.Xna.Framework.Vector3 b)
        {
            float x1 = a.X;
            float y1 = a.Y;
            float z1 = a.Z;
            float x2 = b.X;
            float y2 = b.Y;
            float z2 = b.Z;
            return new Microsoft.Xna.Framework.Vector3(y1 * z2 - z1 * y2, z1 * x2 - x1 * z2, x1 * y2 - y1 * x2);
        }

        public static float dotProduct(Microsoft.Xna.Framework.Vector3 a, Microsoft.Xna.Framework.Vector3 b)
        {
            float x1 = a.X;
            float y1 = a.Y;
            float z1 = a.Z;
            float x2 = b.X;
            float y2 = b.Y;
            float z2 = b.Z;
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        public static void ModelSwapModule()
        {

            System.Windows.Forms.OpenFileDialog openFileDialog1;
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            openFileDialog1.Title = "Choose template seikiro model file.";
            //openFileDialog1.ShowDialog();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Console.WriteLine(openFileDialog1.FileName);
                //openFileDialog1.
            }
            else
            {
                return;
            }

            FLVER2 b = FLVER2.Read(openFileDialog1.FileName);



            System.Windows.Forms.OpenFileDialog openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog2.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            openFileDialog2.Title = "Choose source DS/BB model file.";
            //openFileDialog1.ShowDialog();

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                Console.WriteLine(openFileDialog2.FileName);
                //openFileDialog1.
            }
            else
            {
                return;
            }
            FLVER2 src = FLVER2.Read(openFileDialog2.FileName);



            Console.WriteLine(b.Header);

            Console.WriteLine("Seikiro unk is:" + b.SekiroUnk);



            Console.WriteLine("Material:");
            foreach (FLVER2.Material m in b.Materials)
            {
                Console.WriteLine(m.Name);

            }

            foreach (FLVER2.Mesh m in b.Meshes)
            {
                Console.WriteLine("Mesh#" + m.MaterialIndex);

            }

            //* new
            //b.Header.BigEndian = src.Header.BigEndian;


            //


            //X: is not the sword axis!!!
            //Y: ++ means closer to the hand!
            //Unit: in meter(?)


            //For Moonlight sword -> threaded cane, Y+0.5f

            Form f = new Form();

            Label l = new Label();
            l.Text = "x,y,z offset? Y= weapon length axis,Y+=Closer to hand";
            l.Size = new System.Drawing.Size(150, 15);
            l.Location = new System.Drawing.Point(10, 20);
            f.Controls.Add(l);


            TextBox t = new TextBox();
            t.Size = new System.Drawing.Size(70, 15);
            t.Location = new System.Drawing.Point(10, 60);
            t.Text = "0";
            f.Controls.Add(t);

            TextBox t2 = new TextBox();
            t2.Size = new System.Drawing.Size(70, 15);
            t2.Location = new System.Drawing.Point(10, 100);
            t2.Text = "0";
            f.Controls.Add(t2);

            TextBox t3 = new TextBox();
            t3.Size = new System.Drawing.Size(70, 15);
            t3.Location = new System.Drawing.Point(10, 140);
            t3.Text = "0";
            f.Controls.Add(t3);

            TextBox keepMeshesIndex = new TextBox();
            keepMeshesIndex.Size = new System.Drawing.Size(150, 15);
            keepMeshesIndex.Location = new System.Drawing.Point(100, 60);
            keepMeshesIndex.Text = "";
            f.Controls.Add(keepMeshesIndex);

            CheckBox cb1 = new CheckBox();
            cb1.Size = new System.Drawing.Size(70, 15);
            cb1.Location = new System.Drawing.Point(10, 160);
            cb1.Text = "Copy Material";
            f.Controls.Add(cb1);

            CheckBox cb2 = new CheckBox();
            cb2.Size = new System.Drawing.Size(150, 15);
            cb2.Location = new System.Drawing.Point(10, 180);
            cb2.Text = "Copy Bones";
            f.Controls.Add(cb2);

            CheckBox cb3 = new CheckBox();
            cb3.Size = new System.Drawing.Size(150, 15);
            cb3.Location = new System.Drawing.Point(10, 200);
            cb3.Text = "Copy Dummy";
            f.Controls.Add(cb3);


            CheckBox cb4 = new CheckBox();
            cb4.Size = new System.Drawing.Size(350, 15);
            cb4.Location = new System.Drawing.Point(10, 220);
            cb4.Text = "All vertex weight to first bone";
            f.Controls.Add(cb4);

            f.ShowDialog();

            float x = float.Parse(t.Text);
            float y = float.Parse(t2.Text);
            float z = float.Parse(t3.Text);

            int[] SplitStringIntoInts(string list)
            {
                string[] split = list.Split(new char[1] { ',' });
                List<int> numbers = new List<int>();
                int parsed;

                foreach (string n in split)
                {
                    if (int.TryParse(n, out parsed))
                    {
                        numbers.Add(parsed);
                    }
                }

                return numbers.ToArray();
            }
            int[] meshesToKeep;
            List<int> materialsToKeep = new List<int>();

            // Get a list of meshes to keep from the src flver
            if (keepMeshesIndex.Text.Length > 0)
            {
                // Parse the mesh indices
                meshesToKeep = SplitStringIntoInts(keepMeshesIndex.Text);
                b.Meshes.Clear();
                foreach (int meshIndex in meshesToKeep)
                {
                    b.Meshes.Add(src.Meshes[meshIndex]);
                    materialsToKeep.Add(src.Meshes[meshIndex].MaterialIndex);
                }
            }
            else
            {
                b.Meshes = src.Meshes;
            }

            /*
            if (src.Meshes.Count >= 2)
            {
                b.Meshes.Clear();
                for (int count = 0; count < src.Meshes.Count; count++)
                {
                    if (count == 8)
                    {
                        b.Meshes.Add(src.Meshes[count]);
                    }
                    Console.Write($"{count}");
                    continue;
                }
            }
            */


            if (cb1.Checked) {
                if (materialsToKeep.Count > 0 && materialsToKeep.Count == b.Meshes.Count)
                {
                    b.Materials.Clear();
                    foreach (int material in materialsToKeep)
                    {
                        b.Materials.Add(src.Materials[material]);
                        int materialIndexIter = b.Materials.Count - 1;
                        b.Meshes[materialIndexIter].MaterialIndex = materialIndexIter;
                    }
                }
                else
                {
                    b.Materials = src.Materials;
                }
            }

            if (cb2.Checked)
                b.Bones = src.Bones;

            if (cb3.Checked)
                b.Dummies = src.Dummies;

            if (cb4.Checked)
            {
                for (int i = 0; i < b.Meshes.Count; i++)
                {
                    b.Meshes[i].BoneIndices = new List<int>();
                    b.Meshes[i].BoneIndices.Add(0);
                    b.Meshes[i].BoneIndices.Add(1);
                    b.Meshes[i].DefaultBoneIndex = 1;
                    foreach (FLVER.Vertex v in b.Meshes[i].Vertices)
                    {
                        for (int j = 0; j < v.Position.Length(); j++)
                        {
                            if (v.BoneWeights == null) { continue; }
                            v.Position = new System.Numerics.Vector3(0, 0, 0);
                            for (int k = 0; k < v.BoneWeights.Length; k++)
                            {
                                v.BoneWeights[k] = 0;
                                v.BoneIndices[k] = 0;

                            }
                            v.BoneIndices[0] = 1;
                            v.BoneWeights[0] = 1;
                        }
                    }
                    //targetFlver.Meshes[i].Vertices = new List<FLVER.Vertex>();

                }
            }

            foreach (FLVER2.Mesh m in b.Meshes)
            {
                foreach (FLVER.Vertex v in m.Vertices)
                {
                    v.Position = new System.Numerics.Vector3(v.Position.X + x, v.Position.Y + y, v.Position.Z + z);
                }

            }



            b.Write(openFileDialog1.FileName + "n");
            MessageBox.Show("Swap completed!", "Info");
            //Console.WriteLine("End reading");
            //Application.Exit();

        }

        static ObjLoader.Loader.Data.Elements.FaceVertex[] getVertices(ObjLoader.Loader.Data.Elements.Face f)
        {
            ObjLoader.Loader.Data.Elements.FaceVertex[] ans = new ObjLoader.Loader.Data.Elements.FaceVertex[f.Count];
            for (int i = 0; i < f.Count; i++)
            {
                ans[i] = f[i];
            }
            return ans;
        }

        public static void exportJson(string content, string fileName = "export.json", string endMessage = "")
        {
            var openFileDialog2 = new SaveFileDialog();
            openFileDialog2.FileName = fileName;
            string res = "";
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    var sw = new StreamWriter(openFileDialog2.FileName);
                    sw.Write(content);
                    sw.Close();
                    MessageBox.Show(endMessage, "Info");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }

        }

        public static string FormatOutput(string jsonString)
        {
            var stringBuilder = new StringBuilder();

            bool escaping = false;
            bool inQuotes = false;
            int indentation = 0;

            foreach (char character in jsonString)
            {
                if (escaping)
                {
                    escaping = false;
                    stringBuilder.Append(character);
                }
                else
                {
                    if (character == '\\')
                    {
                        escaping = true;
                        stringBuilder.Append(character);
                    }
                    else if (character == '\"')
                    {
                        inQuotes = !inQuotes;
                        stringBuilder.Append(character);
                    }
                    else if (!inQuotes)
                    {
                        if (character == ',')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', indentation);
                        }
                        else if (character == '[' || character == '{')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', ++indentation);
                        }
                        else if (character == ']' || character == '}')
                        {
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', --indentation);
                            stringBuilder.Append(character);
                        }
                        else if (character == ':')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append('\t');
                        }
                        else
                        {
                            stringBuilder.Append(character);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(character);
                    }
                }
            }

            return stringBuilder.ToString();
        }


    }
}
