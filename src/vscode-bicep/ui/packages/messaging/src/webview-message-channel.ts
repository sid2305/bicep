import { webviewApi } from "./webview-api";

type MessageResponseCallback = (response: unknown, error: unknown) => void;

export class WebviewMessageChannel {
  private readonly callbacks: Record<string, MessageResponseCallback>;
  private readonly onMessage: (messageEvent: MessageEvent) => void;

  constructor() {
    this.callbacks = {};
    this.onMessage = (messageEvent: MessageEvent) => {
      const { requestId, response, error } = messageEvent.data;

      if (!requestId) {
        throw new Error("Expected 'requestId' to be included in message.");
      }

      this.callbacks[requestId]?.(response, error);
    };

    window.addEventListener("message", this.onMessage);
  }

  dispose() {
    window.removeEventListener("message", this.onMessage);
  }

  sendRequest<T>(request: { kind: string }): Promise<T> {
    return new Promise((resolve, reject) => {
      const requestId = window.crypto.randomUUID();

      this.callbacks[requestId] = (response: unknown, error: unknown) => {
        if (error) {
          reject(error);
        } else {
          resolve(response as T);
        }

        if (this.callbacks[requestId]) {
          delete this.callbacks[requestId];
        }
      };

      webviewApi.postMessage({ requestId, ...request });
    });
  }
}

export const webviewMessageChannel = new WebviewMessageChannel();
