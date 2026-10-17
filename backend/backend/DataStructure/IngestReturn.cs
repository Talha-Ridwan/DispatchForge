using backend.Entities;

namespace backend.DataStructure;

public record IngestReturn(Guid EventTypeId, List<Destination> Destinations);