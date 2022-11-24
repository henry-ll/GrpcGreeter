using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcProtos;




// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7080");

//一元模式：客户端发送一个请求，包含两个数字，服务端是返回两个数字的和，这种最基本也是最常用的叫简单模式，
//客户端发送一次，服务端返回一次，类似于接口请求

//服务器流式处理模式：客户端发送一个请求包含两个数字，服务端返回多次，第一次返回两数子和，第二次返回两数字乘，
//这种叫服务端流模式，形象点说就是服务端向客户端搭了一根单向水管，可以不停的往客户端发送数据，
//客户端不停的接收，直到接收到服务端发送的结束标记后停止接收
//使用场景:
//客户端请求一个数据列表，但是这个数据太多了，不可能一次返回，就可以利用这种模式，
//服务端一次返回100条数据，前端一直接收处理

//客户端流式处理模式：客户端发送了很多次数据，数据是单个数字，服务端不停的接收数据，客户端发送结束后服务端返回所有数据的总和，
//这就是客户端流模式，形象点说就是客户端往服务端搭了一个单向的水管，可以不停的往服务端发送数据，
//服务端不停的接收，直到接收到客户端发送的结束标记后停止接收，处理完数据后一次性返回给客户端，

//双向流式处理模式：客户端分多次发送数据给服务端，每次数据是两个数，服务端收到数据后多次返回给服务端，每次返回两个数之和，
//客户端可以多次给服务端发送数据，服务端可以多次返回数据，这就是双向流模式，双方都需要同时处理发送数据和接收数据，
//直到双方通道流关闭


#region 一元模式
//实例化 层级 来源于proto文件内 csharp_namespace -->service名称 Greeter --> service名称 + 当前程序类型（Server | Client）
var greeterClient = new GrpcProtos.Greeter.GreeterClient(channel);
//调用 proto文件内  rpc服务名 SayHello --> 请求参数 为 message体 HelloRequest
var reply = await greeterClient.SayHelloAsync(
                  new HelloRequest { Name = "来自Gprc客户端消息" });
Console.WriteLine("Greeting: " + reply.Message);
#endregion



Dictionary<string, string> fileDic = new Dictionary<string, string>();
fileDic.Add(@"D:\A\242-TYJR-1973-1001(1).zip", @"D:\B\242-TYJR-1973-1001(1)-new.zip");
fileDic.Add(@"D:\A\242-TYJR-1973-1001(2).zip", @"D:\B\242-TYJR-1973-1001(2)-new.zip");

GrpcProtos.GrpcStream.GrpcStreamClient grpcstreamClient;

grpcstreamClient = new GrpcProtos.GrpcStream.GrpcStreamClient(channel);

#region 客户端流式处理方法
await ClientStreamTestAsync();
#endregion

#region 服务器流式处理方法
await ServerStreamTestAsync();
#endregion

#region 双向流式处理方法
await BidirectionalStreamTestAsync();
#endregion

Console.WriteLine("Press any key to exit...");
Console.ReadKey();


//客户端流式处理方法
async Task ClientStreamTestAsync()
{
    var filePath = @"D:\C\client.txt";
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
    }
    using var call = grpcstreamClient.ClientStream();
    var rand = new Random();
    for (int i = 0; i < 10; i++)
    {
        await call.RequestStream.WriteAsync(new ClientStreamRequest
        {
            FilePath = filePath,
            Data = ByteString.CopyFromUtf8(Guid.NewGuid().ToString() + Environment.NewLine)//Environment.NewLine换行符
        });
        await Task.Delay(rand.Next(200));
    }
    await call.RequestStream.CompleteAsync();//完成/关闭流
    var response = await call.ResponseAsync;
    Console.WriteLine(response.Success);
}

//服务器流式处理方法
async Task ServerStreamTestAsync()
{
    var firstFile = fileDic.First();
    if (File.Exists(firstFile.Value))
    {
        File.Delete(firstFile.Value);
    }

    var result = grpcstreamClient.ServerStream(new ServerStreamRequest
    {
        FilePath = firstFile.Key
    });
    var iterator = result.ResponseStream;
    using var fs = new FileStream(firstFile.Value, FileMode.Create);
    while (await iterator.MoveNext())
    {
        Console.WriteLine($"write to new file {iterator.Current.Data.Length} bytes");
        iterator.Current.Data.WriteTo(fs);
    }

}

//双向流式处理方法
async Task BidirectionalStreamTestAsync()
{
    foreach (var newFilePath in fileDic.Values)
    {
        if (File.Exists(newFilePath))
        {
            File.Delete(newFilePath);
        }
    }

    using var call = grpcstreamClient.BidirectionalStream();
    var responseTask = Task.Run(async () =>
    {
        var iterator = call.ResponseStream;
        while (await iterator.MoveNext())
        {
            Console.WriteLine($"write to new file {fileDic[iterator.Current.FilePath]} {iterator.Current.Data.Length} bytes");
            using var fs = new FileStream(fileDic[iterator.Current.FilePath], FileMode.Append);
            iterator.Current.Data.WriteTo(fs);
        }
    });

    var rand = new Random();
    foreach (var item in fileDic)
    {
        await call.RequestStream.WriteAsync(new BidirectionalStreamRequest
        {
            FilePath = item.Key
        });
        await Task.Delay(rand.Next(200));
    }
    await call.RequestStream.CompleteAsync();
    await responseTask;
}

