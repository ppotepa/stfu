# Final CPU/GPU optimization checklist

Required before accepting a performance patch:
- Release run outside debugger.
- worker sweep `1/2/4/8/12/16`.
- CPU full, GPU present and GPU readback measured separately.
- no raw `Parallel.For` outside `STFU.Parallelism`.
- no viewport readback unless requested.
- `ReferenceQuality` worker parity green.
- counters show whether improvement came from less work or faster execution of the same work.
