import { describe, vi, expect, it, afterAll } from "vitest";
import { webviewMessageChannel } from "./webview-message-channel";

interface MockRequestMessage {
  requestId: string;
  kind: "ReturnResponse" | "ReturnError";
}

interface MockResponseMessage {
  requestId: string;
  response?: string;
  error?: string;
}

vi.mock("./webview-api", () => ({
  webviewApi: {
    postMessage: ({ requestId, kind }: MockRequestMessage) => {
      window.postMessage({
        requestId,
        ...(kind === "ReturnResponse"
          ? { response: "Mock response" }
          : { error: "Mock error" }),
      } satisfies MockResponseMessage);
    },
    getState: vi.fn(),
    setState: vi.fn(),
  },
}));

describe("webviewMessageChannel", () => {
  afterAll(() => {
    webviewMessageChannel.dispose();
  });

  describe("sendRequest", () => {
    it("should resolve a response when there is no error", async () => {
      const response =
        await webviewMessageChannel.sendRequest<MockResponseMessage>({
          kind: "ReturnResponse",
        });

      expect(response).toBe("Mock response");
    });

    it("should reject when there is an error", async () => {
      await expect(() =>
        webviewMessageChannel.sendRequest<MockResponseMessage>({
          kind: "ReturnError",
        } satisfies Pick<MockRequestMessage, "kind">),
      ).rejects.toThrow("Mock error");
    });
  });
});
