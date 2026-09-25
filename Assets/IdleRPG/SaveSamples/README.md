# Save samples

Golden save files for the migration tests (Architecture.md rule: every schema bump adds a sample here).

- `v4_sample.json` - a real-shaped v4 file (hero columns + prestige list) that must migrate to v5 with every
  number preserved. Used by `SaveRoundTripMenu` and as the forever-fixture for future migrations (v6...).
