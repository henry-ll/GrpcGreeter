using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcProtos;

Dictionary<string, string> fileDic = new Dictionary<string, string>();
fileDic.Add(@"D:\A\242-TYJR-1973-1001(1).zip", @"D:\B\242-TYJR-1973-1001(1)-new.zip");
fileDic.Add(@"D:\A\242-TYJR-1973-1001(2).zip", @"D:\B\242-TYJR-1973-1001(2)-new.zip");


// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7080");
//实例化 层级 来源于proto文件内 csharp_namespace -->service名称 Greeter --> service名称 + 当前程序类型（Server | Client）
var client = new GrpcProtos.Greeter.GreeterClient(channel);
//调用 proto文件内  rpc服务名 SayHello --> 请求参数 为 message体 HelloRequest
var reply = await client.SayHelloAsync(
                  new HelloRequest { Name = "来自Gprc客户端消息" });
Console.WriteLine("Greeting: " + reply.Message);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();