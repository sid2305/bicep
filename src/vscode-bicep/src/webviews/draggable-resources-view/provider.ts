// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import * as vscode from "vscode";
import crypto from "crypto";

export class DraggableResourcesViewProvider
  implements vscode.WebviewViewProvider
{
  private readonly extensionUri: vscode.Uri;
  private readonly rootUri: vscode.Uri;

  constructor(extensionUri: vscode.Uri) {
    this.extensionUri = extensionUri;
    this.rootUri = vscode.Uri.joinPath(
      this.extensionUri,
      "ui",
      "apps",
      "resource-type-explorer",
      "out",
      "assets",
    );
  }

  resolveWebviewView(webviewView: vscode.WebviewView) {
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [this.rootUri],
    };
    webviewView.webview.html = this.createWebviewHtml(webviewView.webview);
  }

  private getWebviewResourceUri(webview: vscode.Webview, resourceName: string) {
    return webview.asWebviewUri(
      vscode.Uri.joinPath(this.rootUri, resourceName),
    );
  }

  private createWebviewHtml(webview: vscode.Webview): string {
    const { cspSource } = webview;
    const nonce = crypto.randomBytes(16).toString("hex");
    const stylesUri = this.getWebviewResourceUri(webview, "index.css");
    const scriptUri = this.getWebviewResourceUri(webview, "index.js");

    return `
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <!--
        Use a content security policy to only allow loading images from our extension directory,
        and only allow scripts that have a specific nonce.
        -->
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${cspSource} 'unsafe-inline'; img-src ${cspSource} data:; script-src 'nonce-${nonce}' vscode-webview-resource:;">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <link rel="stylesheet" type="text/css" href="${stylesUri}">
      </head>
      <body>
        <div id="root"></div>
        <script nonce="${nonce}" src="${scriptUri}" />
      </body>
      </html>`;
  }
}
