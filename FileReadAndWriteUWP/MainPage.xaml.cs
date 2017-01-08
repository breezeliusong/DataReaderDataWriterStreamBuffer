using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace FileReadAndWriteUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        /// <summary>
        /// 文本内容的文件
        /// </summary>

        //读取本地文件夹根目录的文件
        public async Task<string> ReadFile(string fileName)
        {
            string text;
            try
            {
                IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                var storageFile = await applicationFolder.GetFileAsync(fileName);
                IRandomAccessStream accessStream = await storageFile.OpenReadAsync();
                //使用StreamReader读取文件的内容，需要将IRandomAccessStream对象转化为Stream对象来初始化StreamReader对象
                using (StreamReader streamReader = new StreamReader(accessStream.AsStreamForRead((int)accessStream.Size)))
                {
                    text = streamReader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                text = "文件读取错误" + e.Message;
            }
            return text;
        }

        //写入本地文件夹根目录的文件
        public async Task WriteFile(string fileName, string content)
        {
            IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            IStorageFile storageFile = await applicationFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(storageFile, content);
        }

        ///
        /// 图片文件或者其他二进制文件需要操作文件的Stream或者Buffer数据。
        /// 操作这种二进制文件需要用到DataWriter和DataReader类
        /// DataWriter类用于写入文件的信息，当然这个信息不仅仅是文本信息，各种类型的数据信息都可以写入；
        /// DataReader类则是对应文件的读取类。
        /// 一般对于这种二进制文件，文件写入和读取是要对应起来的。
        /// 例如，文件先写入4个字节的长度，然后再写入文件的内容，这个时候要读取这个文件时，就要先读取4个字节的内容长度，然后再读取实际的内容信息。
        ///Stream 和Buffer的读写操作
        ///

        //Buffer的写入操作
        public async void BufferWrite(IStorageFile file, string content)
        {
            IBuffer buffer;
            int size = Encoding.UTF8.GetByteCount(content);
            using (InMemoryRandomAccessStream memoryStream = new InMemoryRandomAccessStream())
            {
                using (DataWriter dataWriter = new DataWriter(memoryStream))
                {
                    dataWriter.WriteInt32(size);
                    dataWriter.WriteString(content);
                    buffer = dataWriter.DetachBuffer();
                }
            }
            await FileIO.WriteBufferAsync(file, buffer);
        }

        //Buffer 的读取操作
        public async void BufferReader(IStorageFile file)
        {
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            using (DataReader dataReader = DataReader.FromBuffer(buffer))
            {
                //读取文件相关的信息，读取的规则要与文件的规则一致
                Int32 stringSize = dataReader.ReadInt32();
                string fileContent = dataReader.ReadString((uint)stringSize);
            }
        }


        //Stream的写入操作
        //文件的Stream其实就是文件内的信息，所以再用Stream来写入文件的数据时，直接保存Stream的信息就可以，并不需要再调用文件的对象进行保存
        public async void StreamWrite(IStorageFile file,string content)
        {
            int size = Int32.MaxValue;
            using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
            {
                using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                {
                    //文件相关的信息，可以根据文件的规则来进行写入
                    dataWriter.WriteInt32(size);
                    dataWriter.WriteString(content);

                    transaction.Stream.Size = await dataWriter.StoreAsync();
                    //保存Stream数据
                    await transaction.CommitAsync();
                }
            }
        }

        //Stream的读取操作
        //使用Stream读取文件的内容，需要先调用DataReader类的LoadAsync方法，把数据加载进来，再调用相关的Read方法来读取文件的内容；
        //Buffer的操作不用调用LoadAsync方法，那是因为其已经一次性把数据都读取出来了
        public async void StreamReader(IStorageFile file)
        {
            using(IRandomAccessStream readStream=await file.OpenAsync(FileAccessMode.Read))
            {
                using(DataReader dataReader=new DataReader(readStream))
                {
                    //读取文件的相关信息，读取规则要与文件的规则一致
                    await dataReader.LoadAsync(sizeof(Int32));
                    Int32 stringSize = dataReader.ReadInt32();
                    await dataReader.LoadAsync((UInt32)stringSize);
                    string fileContent = dataReader.ReadString((uint)stringSize);
                }
            }
        }

    }
}
