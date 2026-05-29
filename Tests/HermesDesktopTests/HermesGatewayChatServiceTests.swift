import Foundation
import Testing

@testable import HermesDesktop

struct HermesGatewayChatServiceTests {
    @Test
    func bootstrapStatusPrefersNativeOnlyWhenGatewayIsAvailable() {
        var status = HermesChatBootstrapStatus()
        #expect(status.preferredTransportMode == .fallback)

        status.sshConnected = true
        status.pythonAvailable = true
        status.hermesCLIAvailable = true
        status.tuiGatewayAvailable = true
        status.canUseNativeChat = true

        #expect(status.preferredTransportMode == .native)
    }

    @Test
    func requestIDDecodesFromNumbersAndStrings() throws {
        let numeric = try JSONDecoder().decode(HermesGatewayRequestID.self, from: Data("7".utf8))
        let stringy = try JSONDecoder().decode(HermesGatewayRequestID.self, from: Data(#""42""#.utf8))

        #expect(numeric == .int(7))
        #expect(stringy == .string("42"))
        #expect(stringy.intValue == 42)
    }

    @Test
    func rpcClientDeliversReadyPayloadFromEventFrame() async throws {
        let client = HermesGatewayRPCClient()

        async let readyPayload = client.awaitReady(timeout: 1)

        await client.handleStdoutLine(
            #"{"jsonrpc":"2.0","method":"event","params":{"type":"gateway.ready","payload":{"version":"1.2.3","ok":true}}}"#
        )

        let payload = try await readyPayload
        #expect(payload["version"]?.stringValue == "1.2.3")
        #expect(payload["ok"]?.boolValue == true)

        await client.finish()
    }

    @Test
    func rpcClientMatchesRequestResponsesByID() async throws {
        let client = HermesGatewayRPCClient()
        let recorder = SentLineRecorder()
        await client.attachSender { line in
            await recorder.store(line)
        }

        let requestTask = Task {
            try await client.request(
                method: "session.create",
                params: ["client": .string("HermesDesktop")],
                timeout: 1
            )
        }

        let outboundLine = try #require(await recorder.waitForLine())
        let outboundObject = try #require(
            JSONSerialization.jsonObject(with: Data(outboundLine.utf8)) as? [String: Any]
        )
        let requestID = try #require(outboundObject["id"] as? Int)

        await client.handleStdoutLine(
            #"{"jsonrpc":"2.0","id":\#(requestID),"result":{"session_id":"session-123"}}"#
        )

        let response = try await requestTask.value
        #expect(response?.objectValue?["session_id"]?.stringValue == "session-123")

        await client.finish()
    }

    @Test
    func probeNativeChatAvailabilityParsesHealthyRemoteEnvironment() async throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }

        let runner = SequenceSSHProcessRunner(results: [
            SSHCommandResult(stdout: "__hermes_ssh_ok__", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "1", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "hermes 1.0.0", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "1", stderr: "", exitCode: 0)
        ])
        let transport = SSHTransport(
            paths: makeTestAppPaths(root: root),
            processRunner: runner
        )
        let connection = ConnectionProfile(
            label: "Prod",
            sshAlias: "hermes-pi"
        ).updated()

        let status = await transport.probeNativeChatAvailability(on: connection)

        #expect(status.sshConnected)
        #expect(status.pythonAvailable)
        #expect(status.hermesCLIAvailable)
        #expect(status.tuiGatewayAvailable)
        #expect(status.canUseNativeChat)
        #expect(status.hermesVersion == "hermes 1.0.0")
    }

    @Test
    func probeNativeChatAvailabilityReturnsFallbackReasonWhenGatewayImportFails() async throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }

        let runner = SequenceSSHProcessRunner(results: [
            SSHCommandResult(stdout: "__hermes_ssh_ok__", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "1", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "hermes 1.0.0", stderr: "", exitCode: 0),
            SSHCommandResult(stdout: "0", stderr: "", exitCode: 0)
        ])
        let transport = SSHTransport(
            paths: makeTestAppPaths(root: root),
            processRunner: runner
        )
        let connection = ConnectionProfile(
            label: "Prod",
            sshHost: "example.com"
        ).updated()

        let status = await transport.probeNativeChatAvailability(on: connection)

        #expect(!status.canUseNativeChat)
        #expect(status.fallbackReason == "The Hermes TUI gateway is not importable on the remote host.")
    }

    @Test
    func gatewayHistoryDecoderBuildsTranscriptMessagesFromNestedPayload() {
        let payload = JSONValue.object([
            "messages": .array([
                .object([
                    "id": .string("user-1"),
                    "role": .string("user"),
                    "content": .string("Hello Hermes")
                ]),
                .object([
                    "id": .string("assistant-1"),
                    "role": .string("assistant"),
                    "content": .string("Hi there"),
                    "timestamp": .number(1710000000)
                ])
            ])
        ])

        let messages = HermesGatewayHistoryDecoder.sessionMessages(from: payload)

        #expect(messages.count == 2)
        #expect(messages[0].id == "user-1")
        #expect(messages[0].role == .user)
        #expect(messages[0].content == "Hello Hermes")
        #expect(messages[1].id == "assistant-1")
        #expect(messages[1].role == .assistant)
        #expect(messages[1].content == "Hi there")
        #expect(messages[1].timestamp == .unixSeconds(1710000000))
    }

    @Test
    func gatewayHistoryDecoderKeepsStructuredFieldsAsMetadata() {
        let payload = JSONValue.array([
            .object([
                "id": .string("assistant-1"),
                "role": .string("assistant"),
                "content": .string("Done"),
                "model": .string("gpt-5"),
                "finish_reason": .string("stop")
            ])
        ])

        let messages = HermesGatewayHistoryDecoder.sessionMessages(from: payload)

        #expect(messages.count == 1)
        #expect(messages[0].metadata?["model"] == .string("gpt-5"))
        #expect(messages[0].metadata?["finish_reason"] == .string("stop"))
    }

    @Test
    func gatewayHistoryDecoderSkipsToolRoleEntries() {
        let payload = JSONValue.array([
            .object([
                "id": .string("tool-1"),
                "role": .string("tool_result"),
                "content": .string("verbose tool output")
            ]),
            .object([
                "id": .string("assistant-1"),
                "role": .string("assistant"),
                "content": .string("Final answer")
            ])
        ])

        let messages = HermesGatewayHistoryDecoder.sessionMessages(from: payload)

        #expect(messages.count == 1)
        #expect(messages[0].id == "assistant-1")
        #expect(messages[0].content == "Final answer")
    }
}

private actor SentLineRecorder {
    private var line: String?

    func store(_ line: String) {
        self.line = line
    }

    func waitForLine(timeoutNanoseconds: UInt64 = 1_000_000_000) async -> String? {
        let deadline = DispatchTime.now().uptimeNanoseconds + timeoutNanoseconds
        while DispatchTime.now().uptimeNanoseconds < deadline {
            if let line {
                return line
            }
            await Task.yield()
        }
        return line
    }
}

private actor SequenceSSHProcessRunner: SSHProcessRunning {
    private var results: [SSHCommandResult]

    init(results: [SSHCommandResult]) {
        self.results = results
    }

    func run(
        executableURL: URL,
        arguments: [String],
        standardInput: Data?
    ) async throws -> SSHCommandResult {
        guard !results.isEmpty else {
            throw SSHTransportError.localFailure("No more stubbed SSH results.")
        }
        return results.removeFirst()
    }
}
