using HearingTest.Domain.Interfaces;

namespace HearingTest.Hardware;

public class KeyboardResponseInput : IResponseInput
{
    public event EventHandler<int?>? ResponseReceived;

    public void RecordResponse(int? latencyMs = null)
    {
        ResponseReceived?.Invoke(this, latencyMs);
    }
}
