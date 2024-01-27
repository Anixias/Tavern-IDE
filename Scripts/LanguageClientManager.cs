using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Environment = System.Environment;

namespace Tavern;

public static class LanguageClientManager
{
	private static LanguageClient languageClient;

	public static async Task InitializeAsync(string rootPath, CancellationToken cancellationToken)
	{
		const string exePath = @"C:\Users\colnu\OneDrive\Documents\GitHub\CQuence\CQuence\bin\Debug\net7.0\CQuence.exe";
		var languageServer = new Process();
		languageServer.StartInfo.Arguments = $"-parent {Environment.ProcessId}";
		languageServer.StartInfo.FileName = exePath;
		languageServer.StartInfo.RedirectStandardInput = true;
		languageServer.StartInfo.RedirectStandardOutput = true;
		languageServer.StartInfo.UseShellExecute = false;
		languageServer.StartInfo.CreateNoWindow = true;
		if (!languageServer.Start())
		{
			await Console.Error.WriteLineAsync("Failed to start language server.");
			return;
		}

		var inputStream = languageServer.StandardOutput.BaseStream;
		var outputStream = languageServer.StandardInput.BaseStream;

		languageClient = LanguageClient.Create(options =>
			options
				.WithInput(inputStream)
				.WithOutput(outputStream)
				.WithClientInfo(new ClientInfo
				{
					Name = "Tavern IDE",
					Version = "0.0.1"
				})
				.WithTrace(InitializeTrace.Verbose)
				.OnInitialize(async (client, request, token) =>
				{
					request.GetType().GetProperty("ProcessId")?.SetValue(request, (long)Environment.ProcessId);
				})
				.OnInitialized(async (client, request, response, token) =>
				{
					Console.WriteLine("request=" + request);
					Console.WriteLine("response=" + response);
				})
				.WithClientCapabilities(new ClientCapabilities
				{
					Window = new WindowClientCapabilities
					{
						ShowMessage = new Supports<ShowMessageRequestClientCapabilities>(true)
					},
					Workspace = new WorkspaceClientCapabilities
					{
						FileOperations = new Supports<FileOperationsWorkspaceClientCapabilities>(new FileOperationsWorkspaceClientCapabilities
						{
							DidCreate = true,
							DidRename = true,
							DidDelete = true
						})
					},
					TextDocument = new TextDocumentClientCapabilities
					{
						SemanticTokens = new Supports<SemanticTokensCapability>(true),
					}
				})
				.WithRootUri(rootPath));

		await languageClient.Initialize(cancellationToken);
	}

	public static void DidCreate(string path)
	{
		languageClient.DidCreateFile(new DidCreateFileParams
		{
			Files = new Container<FileCreate>(new FileCreate
			{
				Uri = new Uri(path)
			})
		});
	}

	public static void DidDelete(string path)
	{
		languageClient.DidDeleteFile(new DidDeleteFileParams
		{
			Files = new Container<FileDelete>(new FileDelete
			{
				Uri = new Uri(path)
			})
		});
	}

	public static void DidOpen(string path, string text, string languageId)
	{
		languageClient.DidOpenTextDocument(new DidOpenTextDocumentParams
		{
			TextDocument = new TextDocumentItem
			{
				Uri = DocumentUri.File(path),
				LanguageId = languageId,
				Text = text,
				Version = 1
			}
		});
	}
}