// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import * as vscode from "vscode";
import {
  LanguageClient,
  TextDocumentIdentifier,
} from "vscode-languageclient/node";
import { Disposable } from "../utils/disposable";
import { bicepExternalSourceRequestType } from "./protocol";
import * as path from "path";
import { Uri } from "vscode";

export const BicepExternalSourceScheme = "bicep-extsrc";
type ExternalSource = {
  // The title to display for the document,
  //   e.g. "br:myregistry.azurecr.io/myrepo/module/main.json:v1/main.json (module:v1)" or similar
  // VSCode will display everything after the last slash in the title bar, and the full string
  //   on hover.
  title: string;
  // Full module reference, e.g. "myregistry.azurecr.io/myrepo/module:v1"
  moduleReference: string;
  // Full local path to the cached module
  //   e.g. /Users/MyUserName/.bicep/br/myregistry.azurecr.io/myrepo/v1$/main.json
  localPath: string;
};

export class BicepExternalSourceContentProvider
  extends Disposable
  implements vscode.TextDocumentContentProvider
{
  constructor(private readonly languageClient: LanguageClient) {
    super();
    this.register(
      vscode.workspace.onDidOpenTextDocument((document) => {
        /*
         * Changing the language ID while the file is being opened causes one of the following problems:
         * - getting a TextDocument and blocking on it causes a deadlock
         * - doing the same in a fire/forget promise causes strange caching behavior in VS code where
         *   the language server is called for a particular file only once
         * Moving this to an event listener instead avoids these issues entirely.
         */
        this.trySetExternalSourceLanguage(document);
      }),
    );
  }

  onDidChange?: vscode.Event<vscode.Uri> | undefined;

  async provideTextDocumentContent(
    uri: vscode.Uri,
    token: vscode.CancellationToken,
  ): Promise<string> {
    // Ask the language server for the sources for the cached module
    const response = await this.languageClient.sendRequest(
      bicepExternalSourceRequestType,
      this.bicepExternalSourceRequest(uri),
      token,
    );

    return response.content;
  }

  private bicepExternalSourceRequest(uri: vscode.Uri) {
    const { moduleReference, localPath } = this.decodeExternalSourceUri(uri);
    return {
      parentTextDocument: TextDocumentIdentifier.create(localPath),
      target: moduleReference,
    };
  }

  // NOTE: This should match the logic in BicepExternalSourceRequestHandler.GetExternalSourceLinkUri and
  // also bicep\src\Bicep.LangServer.UnitTests\BicepExternalSourceRequestHandlerTests.cs.DecodeExternalSourceUri
  private decodeExternalSourceUri(uri: vscode.Uri): ExternalSource {
    //asdfg test
    // The uri passed in has this format (encoded):
    //   bicep-extsrc:{title}#{module-reference}{encoded-#}{local-cache-file-path}
    const title = decodeURIComponent(uri.path);
    const hashIndex = uri.fragment.indexOf("#");
    const moduleReference = decodeURIComponent(
      uri.fragment.substring(0, hashIndex),
    );
    const localPath = decodeURIComponent(uri.fragment.substring(hashIndex + 1));

    return { title, moduleReference, localPath };
  }

  private getModuleReferenceScheme(uri: Uri): "br" | "ts" {
    // e.g. 'br:registry.azurecr.io/module:v3' => 'br'
    const { moduleReference } = this.decodeExternalSourceUri(uri);

    const colonIndex = moduleReference.indexOf(":");
    if (colonIndex >= 0) {
      const scheme = moduleReference.substring(0, colonIndex);
      if (scheme === "br" || scheme === "ts") {
        return scheme;
      }
    }

    throw new Error(
      `The document URI '${uri.toString()}' is in an unexpected format.`,
    );
  }

  private trySetExternalSourceLanguage(document: vscode.TextDocument): void {
    if (
      document.uri.scheme === BicepExternalSourceScheme &&
      document.languageId === "plaintext"
    ) {
      // The file is showing content from the bicep cache and the language is still set to plain text, so
      // we should try to correct it

      const scheme = this.getModuleReferenceScheme(document.uri);
      const { localPath } = this.decodeExternalSourceUri(document.uri);

      // Not necessary to wait for this to finish
      void vscode.languages.setTextDocumentLanguage(
        document,
        this.getLanguageId(scheme, localPath),
      );
    }
  }

  private getLanguageId(scheme: "br" | "ts", fileName: string) {
    switch (scheme) {
      case "ts":
        return "json";
      case "br": {
        if (path.extname(fileName) === ".bicep") {
          return "bicep";
        }

        const armToolsExtension = vscode.extensions.getExtension(
          "msazurermtools.azurerm-vscode-tools",
        );

        // if ARM Tools extension is installed and active, use a more specific language ID
        // otherwise, fall back to JSON
        return armToolsExtension && armToolsExtension.isActive
          ? "arm-template"
          : "jsonc";
      }
      default:
        return "plaintext";
    }
  }
}
