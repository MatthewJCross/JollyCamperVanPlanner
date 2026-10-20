# JollyCamperVanPlanner

JollyCamperVanPlanner is a Windows desktop application for planning the interior layout of a campervan. It provides a top-down floor plan for positioning and adjusting furniture against the van's dimensions and interior geometry.

## Features

### Van Floor Plan

- Configure van length and width.
- Display a scaled floor plan with a measurement grid.
- Show vehicle features such as the cab, seats, wheel arches and sliding door.
- Display the current drawing scale.

### Furniture Layout

- Add furniture using predefined furniture buttons.
- Select furniture directly on the canvas.
- Move furniture by dragging it.
- Resize furniture using its resize handle.
- Rotate furniture by swapping its length and width.
- Edit furniture names, positions and dimensions.
- Apply changes or delete the selected item.
- Preserve furniture selection when the floor plan is rebuilt or resized.

### Save and Load

- Save layouts to JSON files and load them again.
- Store van length, van width and furniture objects.
- Validate loaded data, including dimensions, numeric values, object bounds and supported rotation angles.

**Current limitation:** The `CamperLayout` model currently stores van length, van width and furniture objects. Other vehicle geometry settings, such as wheel-arch and sliding-door measurements, are not currently saved in the layout file.

### Printing

The application includes work to print the floor plan through the WPF print dialog. Verify the printed output, including scaling and background rendering, before relying on it for workshop measurements.

## Technology

- C#
- WPF (Windows Presentation Foundation)
- .NET
- XAML
- JSON layout serialization
- MVVM-style separation between application state and commands, and canvas rendering and interaction

## Project Structure

The project is being refactored incrementally towards MVVM.

- **`MainWindow.xaml`** — defines the user interface, planner canvas and editor controls.
- **`MainWindow.xaml.cs`** — handles WPF drawing, canvas scaling, selection visuals, mouse interaction, dragging, resizing and window-level operations.
- **`MainViewModel.cs`** — holds van geometry, furniture state, editor properties, commands and coordination events.
- **`FurnitureData`** — represents a furniture item, including its name, position, dimensions and angle.
- **`CamperLayout`** — represents the data written to and read from a layout JSON file.

File names and locations may vary with the current solution structure.

## Building and Running

1. Open the solution in Visual Studio on Windows.
2. Restore NuGet packages if prompted.
3. Build the WPF project.
4. Run the application.

The application targets Windows because it uses WPF.

## Typical Workflow

1. Configure the van dimensions and vehicle geometry.
2. Add the furniture items you want to place.
3. Select an item to edit its name, position and dimensions.
4. Drag items to reposition them, or use the resize handle.
5. Rotate an item to swap its length and width.
6. Save the layout to a JSON file for later use.

## Measurement Notes

- Furniture positions and dimensions are represented in millimetres.
- The floor plan uses a scale to convert millimetres into screen pixels.
- X and Y describe an item's position in the floor-plan coordinate system.
- Furniture is expected to remain within the configured van dimensions.

## Development Notes

- Preserve existing canvas rendering and interaction behaviour when changing ViewModel or command wiring.
- Make incremental changes and test each operation after editing.
- Verify save/load behaviour after changing `CamperLayout` or `FurnitureData`.
- Test printed output separately from the on-screen canvas, since screen colours and editing handles may not be suitable for printing.

## License

No license has been specified. Add a license file and update this section if the project is to be distributed.
