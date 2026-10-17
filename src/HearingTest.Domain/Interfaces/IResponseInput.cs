namespace HearingTest.Domain.Interfaces;

public interface IResponseInput
{
    event EventHandler<int?>? ResponseReceived;
    void RecordResponse(int? latencyMs = null);
}
