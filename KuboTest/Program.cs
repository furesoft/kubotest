using System.IO.MemoryMappedFiles;
using System.Text;
using Ipfs;
using Ipfs.Http;
using OwlCore.Kubo;
using OwlCore.Storage;

namespace KuboTest;

public static class Program
{
    public static async Task Main()
    {
        IFile kuboBinary = await GetKuboBinary();

        var repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KuboTest");
        var bootstrapper = new KuboBootstrapper(kuboBinary, repoPath);
        
        await bootstrapper.StartAsync();

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            bootstrapper.Dispose();
        };

        var ipfsClient = new IpfsClient(bootstrapper.ApiUri.ToString());
        var room = new PeerRoom(await ipfsClient.IdAsync(), ipfsClient.PubSub, "kuboTest");

        room.MessageReceived += MessageReceived;
        
        while (true)
        {
            Console.Write(">> ");
            var input = Console.ReadLine();

            await room.PublishAsync(input);
        }
    }

    private static void MessageReceived(object? sender, IPublishedMessage e)
    {
        Console.WriteLine($"{e.Sender.PublicKey}: {Encoding.Default.GetString(e.DataBytes)}");
    }

    private static async Task<IFile> GetKuboBinary()
    {
        Console.WriteLine("Downloading Binary");
        var downloader = new KuboDownloader();

        var latestKuboBinary = await downloader.DownloadLatestBinaryAsync();

        return latestKuboBinary;
    }
}