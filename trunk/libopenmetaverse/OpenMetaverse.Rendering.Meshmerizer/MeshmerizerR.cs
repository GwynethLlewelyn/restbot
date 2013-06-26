/* Copyright (c) 2008 Robert Adams
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of the copyright holder may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
/*
 * Portions of this code are:
 * Copyright (c) Contributors, http://idealistviewer.org
 * The basic logic of the extrusion code is based on the Idealist viewer code.
 * The Idealist viewer is licensed under the three clause BSD license.
 */
/*
 * MeshmerizerR class implments OpenMetaverse.Rendering.IRendering interface
 * using PrimMesher (http://forge.opensimulator.org/projects/primmesher).
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;

namespace OpenMetaverse.Rendering
{
    /// <summary>
    /// Meshing code based on the Idealist Viewer (20081213).
    /// </summary>
    [RendererName("MeshmerizerR")]
    public class MeshmerizerR : OMVR.IRendering
    {
        /// <summary>
        /// Generates a basic mesh structure from a primitive
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh or null on failure</returns>
        public OMVR.SimpleMesh GenerateSimpleMesh(OMV.Primitive prim, OMVR.DetailLevel lod)
        {
            PrimMesher.PrimMesh newPrim = GeneratePrimMesh(prim, lod, false);
            if (newPrim == null)
                return null;

            SimpleMesh mesh = new SimpleMesh();
            mesh.Path = new Path();
            mesh.Prim = prim;
            mesh.Profile = new Profile();
            mesh.Vertices = new List<Vertex>(newPrim.coords.Count);
            for (int i = 0; i < newPrim.coords.Count; i++)
            {
                PrimMesher.Coord c = newPrim.coords[i];
                mesh.Vertices.Add(new Vertex { Position = new Vector3(c.X, c.Y, c.Z) });
            }

            mesh.Indices = new List<ushort>(newPrim.faces.Count * 3);
            for (int i = 0; i < newPrim.faces.Count; i++)
            {
                PrimMesher.Face face = newPrim.faces[i];
                mesh.Indices.Add((ushort)face.v1);
                mesh.Indices.Add((ushort)face.v2);
                mesh.Indices.Add((ushort)face.v3);
            }

            return mesh;
        }

        /// <summary>
        /// Generates a basic mesh structure from a sculpted primitive
        /// </summary>
        /// <param name="prim">Sculpted primitive to generate the mesh from</param>
        /// <param name="sculptTexture">Sculpt texture</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh or null on failure</returns>
        public OMVR.SimpleMesh GenerateSimpleSculptMesh(OMV.Primitive prim, System.Drawing.Bitmap sculptTexture, OMVR.DetailLevel lod)
        {
            OMVR.FacetedMesh faceted = GenerateFacetedSculptMesh(prim, sculptTexture, lod);

            if (faceted != null && faceted.Faces.Count == 1)
            {
                Face face = faceted.Faces[0];

                SimpleMesh mesh = new SimpleMesh();
                mesh.Indices = face.Indices;
                mesh.Vertices = face.Vertices;
                mesh.Path = faceted.Path;
                mesh.Prim = prim;
                mesh.Profile = faceted.Profile;
                mesh.Vertices = face.Vertices;

                return mesh;
            }

            return null;
        }

        /// <summary>
        /// Generates a a series of faces, each face containing a mesh and
        /// metadata
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh</returns >
        public OMVR.FacetedMesh GenerateFacetedMesh(OMV.Primitive prim, OMVR.DetailLevel lod)
        {
            bool isSphere = ((OMV.ProfileCurve)(prim.PrimData.profileCurve & 0x07) == OMV.ProfileCurve.HalfCircle);
            PrimMesher.PrimMesh newPrim = GeneratePrimMesh(prim, lod, true);
            if (newPrim == null)
                return null;

            int numViewerFaces = newPrim.viewerFaces.Count;
            int numPrimFaces = newPrim.numPrimFaces;

            for (uint i = 0; i < numViewerFaces; i++)
            {
                PrimMesher.ViewerFace vf = newPrim.viewerFaces[(int)i];

                if (isSphere)
                {
                    vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                    vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                    vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                }
            }

            // copy the vertex information into OMVR.IRendering structures
            OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh();
            omvrmesh.Faces = new List<OMVR.Face>();
            omvrmesh.Prim = prim;
            omvrmesh.Profile = new OMVR.Profile();
            omvrmesh.Profile.Faces = new List<OMVR.ProfileFace>();
            omvrmesh.Profile.Positions = new List<OMV.Vector3>();
            omvrmesh.Path = new OMVR.Path();
            omvrmesh.Path.Points = new List<OMVR.PathPoint>();

            Dictionary<OMV.Vector3, int> vertexAccount = new Dictionary<OMV.Vector3, int>();
            for (int ii = 0; ii < numPrimFaces; ii++)
            {
                OMVR.Face oface = new OMVR.Face();
                oface.Vertices = new List<OMVR.Vertex>();
                oface.Indices = new List<ushort>();
                oface.TextureFace = prim.Textures.GetFace((uint)ii);
                int faceVertices = 0;
                vertexAccount.Clear();
                OMV.Vector3 pos;
                int indx;
                OMVR.Vertex vert;
                foreach (PrimMesher.ViewerFace vface in newPrim.viewerFaces)
                {
                    if (vface.primFaceNumber == ii)
                    {
                        faceVertices++;
                        pos = new OMV.Vector3(vface.v1.X, vface.v1.Y, vface.v1.Z);
                        if (vertexAccount.ContainsKey(pos))
                        {
                            // we aleady have this vertex in the list. Just point the index at it
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        }
                        else
                        {
                            // the vertex is not in the list. Add it and the new index.
                            vert = new OMVR.Vertex();
                            vert.Position = pos;
                            vert.TexCoord = new OMV.Vector2(vface.uv1.U, 1.0f - vface.uv1.V);
                            vert.Normal = new OMV.Vector3(vface.n1.X, vface.n1.Y, vface.n1.Z);
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v2.X, vface.v2.Y, vface.v2.Z);
                        if (vertexAccount.ContainsKey(pos))
                        {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        }
                        else
                        {
                            vert = new OMVR.Vertex();
                            vert.Position = pos;
                            vert.TexCoord = new OMV.Vector2(vface.uv2.U, 1.0f - vface.uv2.V);
                            vert.Normal = new OMV.Vector3(vface.n2.X, vface.n2.Y, vface.n2.Z);
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v3.X, vface.v3.Y, vface.v3.Z);
                        if (vertexAccount.ContainsKey(pos))
                        {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        }
                        else
                        {
                            vert = new OMVR.Vertex();
                            vert.Position = pos;
                            vert.TexCoord = new OMV.Vector2(vface.uv3.U, 1.0f - vface.uv3.V);
                            vert.Normal = new OMV.Vector3(vface.n3.X, vface.n3.Y, vface.n3.Z);
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }
                    }
                }
                if (faceVertices > 0)
                {
                    oface.TextureFace = prim.Textures.FaceTextures[ii];
                    if (oface.TextureFace == null)
                    {
                        oface.TextureFace = prim.Textures.DefaultTexture;
                    }
                    oface.ID = ii;
                    omvrmesh.Faces.Add(oface);
                }
            }

            return omvrmesh;
        }

        /// <summary>
        /// Create a sculpty faceted mesh. The actual scuplt texture is fetched and passed to this
        /// routine since all the context for finding teh texture is elsewhere.
        /// </summary>
        /// <returns>The faceted mesh or null if can't do it</returns>
        public OMVR.FacetedMesh GenerateFacetedSculptMesh(OMV.Primitive prim, System.Drawing.Bitmap scupltTexture, OMVR.DetailLevel lod)
        {
            PrimMesher.SculptMesh.SculptType smSculptType;
            switch (prim.Sculpt.Type)
            {
                case OpenMetaverse.SculptType.Cylinder:
                    smSculptType = PrimMesher.SculptMesh.SculptType.cylinder;
                    break;
                case OpenMetaverse.SculptType.Plane:
                    smSculptType = PrimMesher.SculptMesh.SculptType.plane;
                    break;
                case OpenMetaverse.SculptType.Sphere:
                    smSculptType = PrimMesher.SculptMesh.SculptType.sphere;
                    break;
                case OpenMetaverse.SculptType.Torus:
                    smSculptType = PrimMesher.SculptMesh.SculptType.torus;
                    break;
                default:
                    smSculptType = PrimMesher.SculptMesh.SculptType.plane;
                    break;
            }
            // The lod for sculpties is the resolution of the texture passed.
            // The first guess is 1:1 then lower resolutions after that
            // int mesherLod = (int)Math.Sqrt(scupltTexture.Width * scupltTexture.Height);
            int mesherLod = 32; // number used in Idealist viewer
            switch (lod)
            {
                case OMVR.DetailLevel.Highest:
                    break;
                case OMVR.DetailLevel.High:
                    break;
                case OMVR.DetailLevel.Medium:
                    mesherLod /= 2;
                    break;
                case OMVR.DetailLevel.Low:
                    mesherLod /= 4;
                    break;
            }
            PrimMesher.SculptMesh newMesh =
                new PrimMesher.SculptMesh(scupltTexture, smSculptType, mesherLod, true, prim.Sculpt.Mirror, prim.Sculpt.Invert);

            int numPrimFaces = 1;       // a scuplty has only one face

            // copy the vertex information into OMVR.IRendering structures
            OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh();
            omvrmesh.Faces = new List<OMVR.Face>();
            omvrmesh.Prim = prim;
            omvrmesh.Profile = new OMVR.Profile();
            omvrmesh.Profile.Faces = new List<OMVR.ProfileFace>();
            omvrmesh.Profile.Positions = new List<OMV.Vector3>();
            omvrmesh.Path = new OMVR.Path();
            omvrmesh.Path.Points = new List<OMVR.PathPoint>();

            Dictionary<OMVR.Vertex, int> vertexAccount = new Dictionary<OMVR.Vertex, int>();


            for (int ii = 0; ii < numPrimFaces; ii++)
            {
                vertexAccount.Clear();
                OMVR.Face oface = new OMVR.Face();
                oface.Vertices = new List<OMVR.Vertex>();
                oface.Indices = new List<ushort>();
                oface.TextureFace = prim.Textures.GetFace((uint)ii);
                int faceVertices = newMesh.coords.Count;
                OMVR.Vertex vert;

                for (int j = 0; j < faceVertices; j++)
                {
                    vert = new OMVR.Vertex();
                    vert.Position = new Vector3(newMesh.coords[j].X, newMesh.coords[j].Y, newMesh.coords[j].Z);
                    vert.Normal = new Vector3(newMesh.normals[j].X, newMesh.normals[j].Y, newMesh.normals[j].Z);
                    vert.TexCoord = new Vector2(newMesh.uvs[j].U, newMesh.uvs[j].V);
                    oface.Vertices.Add(vert);
                }

                for (int j = 0; j < newMesh.faces.Count; j++)
                {
                    oface.Indices.Add((ushort)newMesh.faces[j].v1);
                    oface.Indices.Add((ushort)newMesh.faces[j].v2);
                    oface.Indices.Add((ushort)newMesh.faces[j].v3);
                }

                if (faceVertices > 0)
                {
                    oface.TextureFace = prim.Textures.FaceTextures[ii];
                    if (oface.TextureFace == null)
                    {
                        oface.TextureFace = prim.Textures.DefaultTexture;
                    }
                    oface.ID = ii;
                    omvrmesh.Faces.Add(oface);
                }
            }

            return omvrmesh;
        }

        /// <summary>
        /// Apply texture coordinate modifications from a
        /// <seealso cref="TextureEntryFace"/> to a list of vertices
        /// </summary>
        /// <param name="vertices">Vertex list to modify texture coordinates for</param>
        /// <param name="center">Center-point of the face</param>
        /// <param name="teFace">Face texture parameters</param>
        public void TransformTexCoords(List<OMVR.Vertex> vertices, OMV.Vector3 center, OMV.Primitive.TextureEntryFace teFace, Vector3 primScale)
        {
            // compute trig stuff up front
            float cosineAngle = (float)Math.Cos(teFace.Rotation);
            float sinAngle = (float)Math.Sin(teFace.Rotation);

            for (int ii = 0; ii < vertices.Count; ii++)
            {
                // tex coord comes to us as a number between zero and one
                // transform about the center of the texture
                OMVR.Vertex vert = vertices[ii];

                // aply planar tranforms to the UV first if applicable
                if (teFace.TexMapType == MappingType.Planar)
                {
                    Vector3 binormal;
                    float d = Vector3.Dot(vert.Normal, Vector3.UnitX);
                    if (d >= 0.5f || d <= -0.5f)
                    {
                        binormal = Vector3.UnitY;
                        if (vert.Normal.X < 0f) binormal *= -1;
                    }
                    else
                    {
                        binormal = Vector3.UnitX;
                        if (vert.Normal.Y > 0f) binormal *= -1;
                    }
                    Vector3 tangent = binormal % vert.Normal;
                    Vector3 scaledPos = vert.Position * primScale;
                    vert.TexCoord.X = 1f + (Vector3.Dot(binormal, scaledPos) * 2f - 0.5f);
                    vert.TexCoord.Y = -(Vector3.Dot(tangent, scaledPos) * 2f - 0.5f);
                }
                
                float repeatU = teFace.RepeatU;
                float repeatV = teFace.RepeatV;
                float tX = vert.TexCoord.X - 0.5f;
                float tY = vert.TexCoord.Y - 0.5f;

                vert.TexCoord.X = (tX * cosineAngle + tY * sinAngle) * repeatU + teFace.OffsetU + 0.5f;
                vert.TexCoord.Y = (-tX * sinAngle + tY * cosineAngle) * repeatV + teFace.OffsetV + 0.5f;
                vertices[ii] = vert;
            }
            return;
        }

        private PrimMesher.PrimMesh GeneratePrimMesh(Primitive prim, DetailLevel lod, bool viewerMode)
        {
            OMV.Primitive.ConstructionData primData = prim.PrimData;
            int sides = 4;
            int hollowsides = 4;

            float profileBegin = primData.ProfileBegin;
            float profileEnd = primData.ProfileEnd;

            bool isSphere = false;

            if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.Circle)
            {
                switch (lod)
                {
                    case OMVR.DetailLevel.Low:
                        sides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
            }
            else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.EqualTriangle)
                sides = 3;
            else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.HalfCircle)
            {
                // half circle, prim is a sphere
                isSphere = true;
                switch (lod)
                {
                    case OMVR.DetailLevel.Low:
                        sides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
                profileBegin = 0.5f * profileBegin + 0.5f;
                profileEnd = 0.5f * profileEnd + 0.5f;
            }

            if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Same)
                hollowsides = sides;
            else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Circle)
            {
                switch (lod)
                {
                    case OMVR.DetailLevel.Low:
                        hollowsides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        hollowsides = 12;
                        break;
                    default:
                        hollowsides = 24;
                        break;
                }
            }
            else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Triangle)
                hollowsides = 3;

            PrimMesher.PrimMesh newPrim = new PrimMesher.PrimMesh(sides, profileBegin, profileEnd, (float)primData.ProfileHollow, hollowsides);
            newPrim.viewerMode = viewerMode;
            newPrim.sphereMode = isSphere;
            newPrim.holeSizeX = primData.PathScaleX;
            newPrim.holeSizeY = primData.PathScaleY;
            newPrim.pathCutBegin = primData.PathBegin;
            newPrim.pathCutEnd = primData.PathEnd;
            newPrim.topShearX = primData.PathShearX;
            newPrim.topShearY = primData.PathShearY;
            newPrim.radius = primData.PathRadiusOffset;
            newPrim.revolutions = primData.PathRevolutions;
            newPrim.skew = primData.PathSkew;
            switch (lod)
            {
                case OMVR.DetailLevel.Low:
                    newPrim.stepsPerRevolution = 6;
                    break;
                case OMVR.DetailLevel.Medium:
                    newPrim.stepsPerRevolution = 12;
                    break;
                default:
                    newPrim.stepsPerRevolution = 24;
                    break;
            }

            if ((primData.PathCurve == OMV.PathCurve.Line) || (primData.PathCurve == OMV.PathCurve.Flexible))
            {
                newPrim.taperX = 1.0f - primData.PathScaleX;
                newPrim.taperY = 1.0f - primData.PathScaleY;
                newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(180 * primData.PathTwist);
                newPrim.ExtrudeLinear();
            }
            else
            {
                newPrim.taperX = primData.PathTaperX;
                newPrim.taperY = primData.PathTaperY;
                newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(360 * primData.PathTwist);
                newPrim.ExtrudeCircular();
            }

            return newPrim;
        }

        /// <summary>
        /// Method for generating mesh Face from a heightmap
        /// </summary>
        /// <param name="zMap">Two dimension array of floats containing height information</param>
        /// <param name="xBegin">Starting value for X</param>
        /// <param name="xEnd">Max value for X</param>
        /// <param name="yBegin">Starting value for Y</param>
        /// <param name="yEnd">Max value of Y</param>
        /// <returns></returns>
        public OMVR.Face TerrainMesh(float[,] zMap, float xBegin, float xEnd, float yBegin, float yEnd)
        {
            PrimMesher.SculptMesh newMesh = new PrimMesher.SculptMesh(zMap, xBegin, xEnd, yBegin, yEnd, true);
            OMVR.Face terrain = new OMVR.Face();
            int faceVertices = newMesh.coords.Count;
            terrain.Vertices = new List<Vertex>(faceVertices);
            terrain.Indices = new List<ushort>(newMesh.faces.Count * 3);

            for (int j = 0; j < faceVertices; j++)
            {
                var vert = new OMVR.Vertex();
                vert.Position = new Vector3(newMesh.coords[j].X, newMesh.coords[j].Y, newMesh.coords[j].Z);
                vert.Normal = new Vector3(newMesh.normals[j].X, newMesh.normals[j].Y, newMesh.normals[j].Z);
                vert.TexCoord = new Vector2(newMesh.uvs[j].U, newMesh.uvs[j].V);
                terrain.Vertices.Add(vert);
            }

            for (int j = 0; j < newMesh.faces.Count; j++)
            {
                terrain.Indices.Add((ushort)newMesh.faces[j].v1);
                terrain.Indices.Add((ushort)newMesh.faces[j].v2);
                terrain.Indices.Add((ushort)newMesh.faces[j].v3);
            }

            return terrain;
        }
    }
}
