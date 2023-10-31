// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import vscode, { Uri } from "vscode";
import { IActionContext } from "@microsoft/vscode-azext-utils";
import { Command } from "./types";

export class ShowMainJsonCommand implements Command {
  public readonly id = "bicep.internal.showJsonSource";
  public disclaimerShownThisSession = false;

  public constructor(
  ) {
    // Nothing to do
  }

  public async execute(
    _context: IActionContext,
    documentUri: Uri,
    targetUri: Uri
  ): Promise<void> {
    var doc = await vscode.workspace.openTextDocument(targetUri);
    await vscode.window.showTextDocument(doc);
  }
}