//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DivisionEngine.Editor;

public partial class ConfirmationDialog : Window
{
    public new string Title { get; set; } = "Confirm";
    public string Message { get; set; } = "Are you sure?";

    public ConfirmationDialog()
    {
        InitializeComponent();
        Width = 350;
        Height = 150;
        DataContext = this;
    }

    private void OnYesClicked(object? obj, RoutedEventArgs args) => Close(true);
    private void OnNoClicked(object? obj, RoutedEventArgs args) => Close(false);
}