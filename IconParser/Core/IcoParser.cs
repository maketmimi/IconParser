using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace IconParser.Core
{
    public class IcoParser : IDisposable
    {
        public static bool CheckIcoFileHeader(byte[] IcoHeader)
        {
            BinaryReader reader = null;

            try
            {
                reader = new BinaryReader(new MemoryStream(IcoHeader));

                return
                    reader.ReadUInt16() == 0 &&
                    reader.ReadUInt16() == 1 &&
                    reader.ReadUInt16() > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                reader?.Close();
            }
        }

        public static bool CheckIcoFileHeader(Stream IcoHeader)
        {
            BinaryReader reader = null;

            try
            {
                reader = new BinaryReader(IcoHeader, Encoding.UTF8, true);

                return CheckIcoFileHeader(reader.ReadBytes(6));
            }
            catch
            {
                return false;
            }
            finally
            {
                reader?.Close();
            }
        }

        public static bool CheckIcoFileHeader(string FileName)
        {
            FileStream IcoFile = null;

            try
            {
                IcoFile = new FileStream(FileName, FileMode.Open);

                return CheckIcoFileHeader(IcoFile);
            }
            catch
            {
                return false;
            }
            finally
            {
                IcoFile?.Close();
            }
        }

        public ushort NumberOfImagesInCurrentIcoFile { get; private set; } = 0;
        private readonly List<Image> _IcoImages = new List<Image>();
        private int _CurrentImageIndex = -1;
        public int CurrentImageNumber
        {
            get
            {
                return (_CurrentImageIndex == -1 ? 0 : _CurrentImageIndex) + 1;
            }
        }

        public IcoParser(string fileName)
        {
            FileStream IcoFile = null;

            try
            {
                IcoFile = new FileStream(fileName, FileMode.Open);
                
                ParseIcoFile(IcoFile);
            }
            catch
            {
                this.Dispose();
                throw new InvalidDataException($"Invalid Ico File: {fileName}");
            }
            finally
            {
                IcoFile?.Close();
            }
        }

        public IcoParser(Stream IcoFile)
        {
            try
            {
                ParseIcoFile(IcoFile);
            }
            catch
            {
                this.Dispose();
                throw new InvalidDataException($"Invalid Ico File stream!");
            }
        }

        private void ParseIcoFile(Stream IcoFileToParse)
        {
            ParseIcoHeader(IcoFileToParse);
            ParseIcoEntries(IcoFileToParse);
        }

        private void ParseIcoHeader(Stream IcoFileToParse)
        {
            BinaryReader reader = new BinaryReader(IcoFileToParse, Encoding.UTF8, true);
            
            if (reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1)
            {
                throw new InvalidDataException("Invalid Ico file format!");
            }

            NumberOfImagesInCurrentIcoFile = reader.ReadUInt16();

            if (NumberOfImagesInCurrentIcoFile <= 0)
            {
                throw new InvalidDataException("Invalid Ico file format!");
            }

            reader.Close();
        }

        private void ParseIcoEntry(Stream IcoFileToParse, byte[] EntryToParse)
        {
            BinaryReader EntryReader = null;
            BinaryReader IcoFileReader = null;
            Image IcoImage = null;

            try
            {
                EntryReader = new BinaryReader(new MemoryStream(EntryToParse));

                short width = (width = EntryReader.ReadByte()) == 0 ? (short) 256 : width;  
                short height = (height = EntryReader.ReadByte()) == 0 ? (short)256 : width;

                EntryReader.BaseStream.Seek(8, SeekOrigin.Begin);
                uint IcoImageLength = EntryReader.ReadUInt32();
                uint IcoImageDataOffeet = EntryReader.ReadUInt32();

                IcoFileReader = new BinaryReader(IcoFileToParse, Encoding.UTF8, true);
            
                IcoFileReader.BaseStream.Seek(IcoImageDataOffeet, SeekOrigin.Begin);

                byte[] IcoImageData = IcoFileReader.ReadBytes(((int)IcoImageLength));
            
                IcoImage = Image.FromStream(new MemoryStream(IcoImageData));

                _IcoImages.Add(new Bitmap(IcoImage, width, height));
            }
            finally
            {
                EntryReader?.Close();
                IcoFileReader?.Close();
                IcoImage?.Dispose();
            }
        }

        private void ParseIcoEntries(Stream IcoFileToParse)
        {
            BinaryReader reader = new BinaryReader(IcoFileToParse, Encoding.UTF8, true);

            for (int i = 0; i < NumberOfImagesInCurrentIcoFile; i++)
            {
                reader.BaseStream.Seek((6 + (16 * i)), SeekOrigin.Begin);
                ParseIcoEntry(IcoFileToParse, reader.ReadBytes(16));
            }

            reader.Close();
        }

        /// <summary>
        /// this returns the image at a specified index in the ico file 
        /// note that you need to manually dispose the returned image object
        /// </summary>
        /// <returns>
        /// the image at a specified index in the ico file or null if the index is out of bounds 
        /// </returns>
        public Image GetImageAt(uint index)
        {
            if (index >= _IcoImages.Count) return null;

            return (Image) _IcoImages[((int)index)].Clone();
        }

        /// <summary>
        /// this returns the next image in the ico file 
        /// note that you need to manually dispose the returned image object
        /// </summary>
        /// <returns>
        /// the next image in the ico file or null if no more next images in the file
        /// </returns>
        public Image GetNextImage()
        {
            if (_CurrentImageIndex == (_IcoImages.Count - 1)) return null;

            return GetImageAt((uint)++_CurrentImageIndex);
        }

        /// <summary>
        /// this returns the previous image in the ico file 
        /// note that you need to manually dispose the returned image object
        /// </summary>
        /// <returns>
        /// the previous image in the ico file or null if no more previous images in the file
        /// </returns>
        public Image GetPreviousImage()
        {
            if (_CurrentImageIndex == -1 || _CurrentImageIndex == 0) return null;

            return GetImageAt((uint)--_CurrentImageIndex);
        }

        /// <summary>
        /// this method gives you all images in the ico file
        /// note that you should dispose the returned images
        /// </summary>
        /// <returns>
        /// an array containing all images in the ico file
        /// </returns>
        public Image[] GetAllImages()
        {
            Image[] ArrToReturn = new Image[NumberOfImagesInCurrentIcoFile];

            for (int i = 0; i < _IcoImages.Count; i++)
            {
                ArrToReturn[i] = (Image) _IcoImages[i].Clone();
            }

            return ArrToReturn;
        }

        public void Dispose()
        {
            if (_IcoImages != null)
            {
                foreach (var image in _IcoImages)
                {
                    image?.Dispose();
                }
            }
        }
    }
}
