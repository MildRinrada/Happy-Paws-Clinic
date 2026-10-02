using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Common.Errors;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// Every endpoint shares one error envelope. These tests assert the mapping from each expected
/// failure to its status, code and details — and that unexpected failures leak nothing.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ErrorEnvelopeTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(HttpStatusCode Status, JsonElement Error)> ThrowAsync(string kind)
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync($"/_probe/throw/{kind}", Token);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        return (response.StatusCode, body.GetProperty("error"));
    }

    [Fact]
    public async Task Every_error_carries_the_contract_envelope_fields()
    {
        var (_, error) = await ThrowAsync("not-found");

        Assert.True(error.TryGetProperty("code", out _));
        Assert.True(error.TryGetProperty("message", out _));
        Assert.True(error.TryGetProperty("request_id", out _));
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("request_id").GetString()));
    }

    [Fact]
    public async Task Validation_failure_is_422_and_names_the_offending_field()
    {
        var (status, error) = await ThrowAsync("validation");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Equal(ErrorCodes.ValidationFailed, error.GetProperty("code").GetString());
        Assert.Equal("starts_at", error.GetProperty("field").GetString());
        Assert.True(error.GetProperty("details").TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Not_found_is_404_and_does_not_echo_the_id_that_was_asked_for()
    {
        // Echoing the id back would confirm to a probing caller that it exists — which is exactly
        // what answering 404 instead of 403 was meant to avoid.
        var (status, error) = await ThrowAsync("not-found");

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal(ErrorCodes.NotFound, error.GetProperty("code").GetString());
        Assert.DoesNotContain("apt_secret", error.ToString());
    }

    [Fact]
    public async Task Forbidden_is_403_and_does_not_leak_the_internal_reason()
    {
        var (status, error) = await ThrowAsync("forbidden");

        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Equal(ErrorCodes.Forbidden, error.GetProperty("code").GetString());
        Assert.DoesNotContain("probe reason that must not leak", error.ToString());
    }

    [Fact]
    public async Task Version_conflict_is_409_and_reports_both_versions()
    {
        var (status, error) = await ThrowAsync("version-conflict");

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(ErrorCodes.VersionConflict, error.GetProperty("code").GetString());

        var details = error.GetProperty("details");
        Assert.Equal(7, details.GetProperty("expected_version").GetInt32());
        Assert.Equal(9, details.GetProperty("current_version").GetInt32());
    }

    [Fact]
    public async Task Signed_record_is_423_record_locked()
    {
        var (status, error) = await ThrowAsync("record-locked");

        Assert.Equal(HttpStatusCode.Locked, status);
        Assert.Equal(ErrorCodes.RecordLocked, error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Illegal_transition_is_409_state_conflict_with_the_states()
    {
        var (status, error) = await ThrowAsync("state-conflict");

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(ErrorCodes.StateConflict, error.GetProperty("code").GetString());

        var details = error.GetProperty("details");
        Assert.Equal("completed", details.GetProperty("current_state").GetString());
        Assert.Equal("cancel", details.GetProperty("attempted").GetString());
    }

    [Fact]
    public async Task Insufficient_scope_thrown_from_a_use_case_matches_the_middleware_shape()
    {
        // A 403 raised deep in a use case must be indistinguishable from one raised by the
        // authorization middleware; clients parse one structure.
        var (status, error) = await ThrowAsync("insufficient-scope");

        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Equal(ErrorCodes.InsufficientScope, error.GetProperty("code").GetString());
        Assert.True(error.GetProperty("details").TryGetProperty("missing_scopes", out _));
    }

    [Fact]
    public async Task An_unexpected_fault_is_500_and_leaks_nothing()
    {
        // In a system holding medical records, an exception message echoed to the caller is a
        // data disclosure. The client gets a code and a request id; the detail goes to the log.
        var (status, error) = await ThrowAsync("boom");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal(ErrorCodes.InternalError, error.GetProperty("code").GetString());

        var serialised = error.ToString();
        Assert.DoesNotContain("secret", serialised);
        Assert.DoesNotContain("InvalidOperationException", serialised);
        Assert.DoesNotContain("at HappyPaws", serialised);
    }

    [Fact]
    public async Task Thai_messages_are_not_unicode_escaped()
    {
        // The envelope's message is displayed to end users. Escaped ช... would force every
        // client to decode before rendering.
        using var client = fixture.CreateClient();

        // GetStringAsync would throw on the 422 this deliberately returns.
        var response = await client.GetAsync("/_probe/throw/validation", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);

        Assert.Contains("ต้องไม่เป็นอดีต", raw);
        Assert.DoesNotContain("\\u0e", raw);
    }
}
