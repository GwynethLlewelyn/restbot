/*
 * Copyright (c) 2007, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;

namespace libsecondlife
{
    public abstract partial class LLObject
    {
        #region Enumerations

        /// <summary>
        /// The type of bump-mapping applied to a face
        /// </summary>
        public enum Bumpiness : byte
        {
            /// <summary></summary>
            None = 0,
            /// <summary></summary>
            Brightness = 1,
            /// <summary></summary>
            Darkness = 2,
            /// <summary></summary>
            Woodgrain = 3,
            /// <summary></summary>
            Bark = 4,
            /// <summary></summary>
            Bricks = 5,
            /// <summary></summary>
            Checker = 6,
            /// <summary></summary>
            Concrete = 7,
            /// <summary></summary>
            Crustytile = 8,
            /// <summary></summary>
            Cutstone = 9,
            /// <summary></summary>
            Discs = 10,
            /// <summary></summary>
            Gravel = 11,
            /// <summary></summary>
            Petridish = 12,
            /// <summary></summary>
            Siding = 13,
            /// <summary></summary>
            Stonetile = 14,
            /// <summary></summary>
            Stucco = 15,
            /// <summary></summary>
            Suction = 16,
            /// <summary></summary>
            Weave = 17
        }

        /// <summary>
        /// The level of shininess applied to a face
        /// </summary>
        public enum Shininess
        {
            /// <summary></summary>
            None = 0,
            /// <summary></summary>
            Low = 0x40,
            /// <summary></summary>
            Medium = 0x80,
            /// <summary></summary>
            High = 0xC0
        }

        /// <summary>
        /// The texture mapping style used for a face
        /// </summary>
        public enum Mapping
        {
            /// <summary></summary>
            Default = 0,
            /// <summary></summary>
            Planar = 2
        }

        /// <summary>
        /// Flags in the TextureEntry block that describe which properties are 
        /// set
        /// </summary>
        [Flags]
        public enum TextureAttributes : uint
        {
            /// <summary></summary>
            None = 0,
            /// <summary></summary>
            TextureID = 1 << 0,
            /// <summary></summary>
            RGBA = 1 << 1,
            /// <summary></summary>
            RepeatU = 1 << 2,
            /// <summary></summary>
            RepeatV = 1 << 3,
            /// <summary></summary>
            OffsetU = 1 << 4,
            /// <summary></summary>
            OffsetV = 1 << 5,
            /// <summary></summary>
            Rotation = 1 << 6,
            /// <summary></summary>
            Material = 1 << 7,
            /// <summary></summary>
            Media = 1 << 8,
            /// <summary></summary>
            All = 0xFFFFFFFF
        }

        #endregion Enumerations

        /// <summary>
        /// Represents all of the texturable faces for an object
        /// </summary>
        /// <remarks>Objects in Second Life have infinite faces, with each face
        /// using the properties of the default face unless set otherwise. So if
        /// you have a TextureEntry with a default texture uuid of X, and face 72
        /// has a texture UUID of Y, every face would be textured with X except for
        /// face 72 that uses Y. In practice however, primitives utilize a maximum
        /// of nine faces and avatars utilize</remarks>
        [Serializable]
        public class TextureEntry2
        {
            /// <summary></summary>
            public TextureEntryFace DefaultTexture;
            /// <summary></summary>
            public TextureEntryFace[] FaceTextures;

            private const int MAX_FACES = 32;

            /// <summary>
            /// Default constructor, DefaultTexture will be null
            /// </summary>
            //public TextureEntry2()
            //{
            //    DefaultTexture = null;
            //    FaceTextures = new TextureEntryFace[0];
            //}

            /// <summary>
            /// Constructor that takes a default texture UUID
            /// </summary>
            /// <param name="defaultTextureID">Texture UUID to use as the default texture</param>
            public TextureEntry2(LLUUID defaultTextureID)
            {
                DefaultTexture = new TextureEntryFace(null);
                DefaultTexture.TextureID = defaultTextureID;
                FaceTextures = new TextureEntryFace[MAX_FACES];
            }

            /// <summary>
            /// Constructor that creates the TextureEntry class from a byte array
            /// </summary>
            /// <param name="data">Byte array containing the TextureEntry field</param>
            /// <param name="pos">Starting position of the TextureEntry field in 
            /// the byte array</param>
            /// <param name="length">Length of the TextureEntry field, in bytes</param>
            public TextureEntry2(byte[] data, int pos, int length)
            {
                FromBytes(data, pos, length);
            }

            /// <summary>
            /// This will either create a new face if a custom face for the given
            /// index is not defined, or return the custom face for that index if
            /// it already exists
            /// </summary>
            /// <param name="index">The index number of the face to create or 
            /// retrieve</param>
            /// <returns>A TextureEntryFace containing all the properties for that
            /// face</returns>
            public TextureEntryFace CreateFace(uint index)
            {
                if (index >= MAX_FACES) throw new Exception(index + " is outside the range of MAX_FACES");

                if (FaceTextures[index] == null)
                    FaceTextures[index] = new TextureEntryFace(this.DefaultTexture);

                return FaceTextures[index];
            }

            private void FromBytes(byte[] data, int pos, int length)
            {
                if (length <= 0)
                {
                    // No TextureEntry to process
                    DefaultTexture = null;
                    FaceTextures = new TextureEntryFace[0];
                    return;
                }
                else
                {
                    DefaultTexture = new TextureEntryFace(null);
                    FaceTextures = new TextureEntryFace[MAX_FACES];
                }

                uint bitfieldSize = 0;
                uint faceBits = 0;
                int i = pos;

                #region Texture
                DefaultTexture.TextureID = new LLUUID(data, i);
                i += 16;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    LLUUID tmpUUID = new LLUUID(data, i);
                    i += 16;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).TextureID = tmpUUID;
                }
                #endregion Texture

                #region Color
                DefaultTexture.RGBA = (uint)(data[i] + (data[i + 1] << 8) + (data[i + 2] << 16) + (data[i + 3] << 24));
                i += 4;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    uint tmpUint = (uint)(data[i] + (data[i + 1] << 8) + (data[i + 2] << 16) + (data[i + 3] << 24));
                    i += 4;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).RGBA = tmpUint;
                }
                #endregion Color

                #region RepeatU
                DefaultTexture.RepeatU = Helpers.BytesToFloat(data, i);
                i += 4;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    float tmpFloat = Helpers.BytesToFloat(data, i);
                    i += 4;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).RepeatU = tmpFloat;
                }
                #endregion RepeatU

                #region RepeatV
                DefaultTexture.RepeatV = Helpers.BytesToFloat(data, i);
                i += 4;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    float tmpFloat = Helpers.BytesToFloat(data, i);
                    i += 4;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).RepeatV = tmpFloat;
                }
                #endregion RepeatV

                #region OffsetU
                DefaultTexture.OffsetU = Helpers.TEOffsetFloat(data, i);
                i += 2;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    float tmpFloat = Helpers.TEOffsetFloat(data, i);
                    i += 2;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).OffsetU = tmpFloat;
                }
                #endregion OffsetU

                #region OffsetV
                DefaultTexture.OffsetV = Helpers.TEOffsetFloat(data, i);
                i += 2;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    float tmpFloat = Helpers.TEOffsetFloat(data, i);
                    i += 2;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).OffsetV = tmpFloat;
                }
                #endregion OffsetV

                #region Rotation
                DefaultTexture.Rotation = Helpers.TERotationFloat(data, i);
                i += 2;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    float tmpFloat = Helpers.TERotationFloat(data, i);
                    i += 2;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).Rotation = tmpFloat;
                }
                #endregion Rotation

                #region Material
                DefaultTexture.material = data[i];
                i++;

                while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    byte tmpByte = data[i];
                    i++;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).material = tmpByte;
                }
                #endregion Material

                #region Media
                DefaultTexture.media = data[i];
                i++;

                while (i - pos < length && ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    byte tmpByte = data[i];
                    i++;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                        if ((faceBits & bit) != 0)
                            CreateFace(face).media = tmpByte;
                }
                #endregion Media
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public byte[] ToBytes()
            {
                if (DefaultTexture == null)
                    return new byte[0];

                MemoryStream memStream = new MemoryStream();
                BinaryWriter binWriter = new BinaryWriter(memStream);

                #region Bitfield Setup

                uint[] textures = new uint[FaceTextures.Length];
                InitializeArray(ref textures);
                uint[] rgbas = new uint[FaceTextures.Length];
                InitializeArray(ref rgbas);
                uint[] repeatus = new uint[FaceTextures.Length];
                InitializeArray(ref repeatus);
                uint[] repeatvs = new uint[FaceTextures.Length];
                InitializeArray(ref repeatvs);
                uint[] offsetus = new uint[FaceTextures.Length];
                InitializeArray(ref offsetus);
                uint[] offsetvs = new uint[FaceTextures.Length];
                InitializeArray(ref offsetvs);
                uint[] rotations = new uint[FaceTextures.Length];
                InitializeArray(ref rotations);
                uint[] materials = new uint[FaceTextures.Length];
                InitializeArray(ref materials);
                uint[] medias = new uint[FaceTextures.Length];
                InitializeArray(ref medias);

                for (int i = 0; i < FaceTextures.Length; i++)
                {
                    if (FaceTextures[i] == null) continue;

                    if (FaceTextures[i].TextureID != DefaultTexture.TextureID)
                    {
                        if (textures[i] == UInt32.MaxValue) textures[i] = 0;
                        textures[i] |= (uint)(1 << i);
                    }
                    if (FaceTextures[i].RGBA != DefaultTexture.RGBA)
                    {
                        if (rgbas[i] == UInt32.MaxValue) rgbas[i] = 0;
                        rgbas[i] |= (uint)(1 << i);
                    }
                    if (FaceTextures[i].RepeatU != DefaultTexture.RepeatU)
                    {
                        if (repeatus[i] == UInt32.MaxValue) repeatus[i] = 0;
                        repeatus[i] |= (uint)(1 << i);
                    }
                    if (FaceTextures[i].RepeatV != DefaultTexture.RepeatV)
                    {
                        if (repeatvs[i] == UInt32.MaxValue) repeatvs[i] = 0;
                        repeatvs[i] |= (uint)(1 << i);
                    }
                    if (Helpers.TEOffsetShort(FaceTextures[i].OffsetU) != Helpers.TEOffsetShort(DefaultTexture.OffsetU))
                    {
                        if (offsetus[i] == UInt32.MaxValue) offsetus[i] = 0;
                        offsetus[i] |= (uint)(1 << i);
                    }
                    if (Helpers.TEOffsetShort(FaceTextures[i].OffsetV) != Helpers.TEOffsetShort(DefaultTexture.OffsetV))
                    {
                        if (offsetvs[i] == UInt32.MaxValue) offsetvs[i] = 0;
                        offsetvs[i] |= (uint)(1 << i);
                    }
                    if (Helpers.TERotationShort(FaceTextures[i].Rotation) != Helpers.TERotationShort(DefaultTexture.Rotation))
                    {
                        if (rotations[i] == UInt32.MaxValue) rotations[i] = 0;
                        rotations[i] |= (uint)(1 << i);
                    }
                    if (FaceTextures[i].material != DefaultTexture.material)
                    {
                        if (materials[i] == UInt32.MaxValue) materials[i] = 0;
                        materials[i] |= (uint)(1 << i);
                    }
                    if (FaceTextures[i].media != DefaultTexture.media)
                    {
                        if (medias[i] == UInt32.MaxValue) medias[i] = 0;
                        medias[i] |= (uint)(1 << i);
                    }
                }

                #endregion Bitfield Setup

                #region Texture
                binWriter.Write(DefaultTexture.TextureID.GetBytes());
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(textures[i]));
                        binWriter.Write(FaceTextures[i].TextureID.GetBytes());
                    }
                }
                binWriter.Write((byte)0);
                #endregion Texture

                #region Color
                binWriter.Write(DefaultTexture.RGBA);
                for (int i = 0; i < rgbas.Length; i++)
                {
                    if (rgbas[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(rgbas[i]));
                        binWriter.Write(FaceTextures[i].RGBA);
                    }
                }
                binWriter.Write((byte)0);
                #endregion Color

                #region RepeatU
                binWriter.Write(DefaultTexture.RepeatU);
                for (int i = 0; i < repeatus.Length; i++)
                {
                    if (repeatus[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(repeatus[i]));
                        binWriter.Write(FaceTextures[i].RepeatU);
                    }
                }
                binWriter.Write((byte)0);
                #endregion RepeatU

                #region RepeatV
                binWriter.Write(DefaultTexture.RepeatV);
                for (int i = 0; i < repeatvs.Length; i++)
                {
                    if (repeatvs[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(repeatvs[i]));
                        binWriter.Write(FaceTextures[i].RepeatV);
                    }
                }
                binWriter.Write((byte)0);
                #endregion RepeatV

                #region OffsetU
                binWriter.Write(Helpers.TEOffsetShort(DefaultTexture.OffsetU));
                for (int i = 0; i < offsetus.Length; i++)
                {
                    if (offsetus[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(offsetus[i]));
                        binWriter.Write(Helpers.TEOffsetShort(FaceTextures[i].OffsetU));
                    }
                }
                binWriter.Write((byte)0);
                #endregion OffsetU

                #region OffsetV
                binWriter.Write(Helpers.TEOffsetShort(DefaultTexture.OffsetV));
                for (int i = 0; i < offsetvs.Length; i++)
                {
                    if (offsetvs[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(offsetvs[i]));
                        binWriter.Write(Helpers.TEOffsetShort(FaceTextures[i].OffsetV));
                    }
                }
                binWriter.Write((byte)0);
                #endregion OffsetV

                #region Rotation
                binWriter.Write(Helpers.TERotationShort(DefaultTexture.Rotation));
                for (int i = 0; i < rotations.Length; i++)
                {
                    if (rotations[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(rotations[i]));
                        binWriter.Write(Helpers.TERotationShort(FaceTextures[i].Rotation));
                    }
                }
                binWriter.Write((byte)0);
                #endregion Rotation

                #region Material
                binWriter.Write(DefaultTexture.material);
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(materials[i]));
                        binWriter.Write(FaceTextures[i].material);
                    }
                }
                binWriter.Write((byte)0);
                #endregion Material

                #region Media
                binWriter.Write(DefaultTexture.media);
                for (int i = 0; i < medias.Length; i++)
                {
                    if (medias[i] != UInt32.MaxValue)
                    {
                        binWriter.Write(GetFaceBitfieldBytes(medias[i]));
                        binWriter.Write(FaceTextures[i].media);
                    }
                }
                #endregion Media

                return memStream.ToArray();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string output = String.Empty;

                output += "Default Face: " + DefaultTexture.ToString() + Environment.NewLine;

                for (int i = 0; i < FaceTextures.Length; i++)
                {
                    if (FaceTextures[i] != null)
                        output += "Face " + i + ": " + FaceTextures[i].ToString() + Environment.NewLine;
                }

                return output;
            }

            private void InitializeArray(ref uint[] array)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = UInt32.MaxValue;
            }

            private bool ReadFaceBitfield(byte[] data, ref int pos, ref uint faceBits, ref uint bitfieldSize)
            {
                faceBits = 0;
                bitfieldSize = 0;

                if (pos >= data.Length)
                    return false;

                byte b = 0;
                do
                {
                    b = data[pos];
                    faceBits = (faceBits << 7) | (uint)(b & 0x7F);
                    bitfieldSize += 7;
                    pos++;
                }
                while ((b & 0x80) != 0);

                return (faceBits != 0);
            }

            private byte[] GetFaceBitfieldBytes(uint bitfield)
            {
                int byteLength = 0;
                uint tmpBitfield = bitfield;
                while (tmpBitfield != 0)
                {
                    tmpBitfield >>= 7;
                    byteLength++;
                }

                if (byteLength == 0)
                    return new byte[1] { 0 };

                byte[] bytes = new byte[byteLength];
                for (int i = 0; i < byteLength; i++)
                {
                    bytes[i] = (byte)((bitfield >> (7 * (byteLength - i - 1))) & 0x7F);
                    if (i < byteLength - 1)
                        bytes[i] |= 0x80;
                }
                return bytes;
            }
        }
    }
}
