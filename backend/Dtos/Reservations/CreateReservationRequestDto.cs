/*
 * ---------------------------------------------------------------------------
 * File        : CreateReservationRequestDto.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-19
 * Description : A request to book an energy trading window, sent by the mobile
 *               application, or by a grid operator booking on a prosumer's
 *               behalf from the web application.
 *
 * Security    : ProsumerNic is optional and is IGNORED when the caller is a
 *               prosumer: their own NIC is taken from the signed token instead.
 *               Only staff may supply it, and only so they can book for
 *               somebody at the counter. Accepting it from a prosumer would let
 *               one prosumer create bookings in another's name.
 * Business    : The reservation date is not accepted here. It is derived from
 *               the chosen window, so a caller cannot claim a booking is at a
 *               time the node is closed, and BR-1 always measures against the
 *               real slot time.
 * ---------------------------------------------------------------------------
 */

using System.ComponentModel.DataAnnotations;

namespace SolarMicrogrid.Api.Dtos.Reservations;

/// <summary>
/// Details of a new energy slot booking.
/// </summary>
public sealed class CreateReservationRequestDto
{
    /// <summary>Identifier of the booking window being reserved.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Slot id is required.")]
    public string SlotId { get; init; } = string.Empty;

    /// <summary>Energy to be traded during the window, in kilowatt hours.</summary>
    [Range(0.1, 1000, ErrorMessage = "Energy must be between 0.1 and 1000 kWh.")]
    public double EnergyKwh { get; init; }

    /// <summary>
    /// Whether the prosumer is delivering energy into the grid or drawing it.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Direction is required.")]
    [RegularExpression("^(Deliver|Draw)$", ErrorMessage = "Direction must be either Deliver or Draw.")]
    public string Direction { get; init; } = string.Empty;

    /// <summary>
    /// NIC of the prosumer the booking is for. Used only when a staff member
    /// books on someone's behalf; ignored when a prosumer books for themselves.
    /// </summary>
    public string? ProsumerNic { get; init; }
}
