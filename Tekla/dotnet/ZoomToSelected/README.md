# Zoom To Selected

## Description
Zoom To Selected is a extension that enables users to temporarily show hidden model objects in a drawing to make them selectable for the zoom operation. After zooming, the objects are automatically hidden again.


## Problem Solved
In Tekla Structures drawings, hidden objects cannot be selected for zooming. This extension solves this problem by temporarily making the selected hidden objects visible, allowing you to zoom to them, and then hiding them again automatically.

# 

## How to Use

1. Open a Tekla Structures drawing

2. Select the hidden object(s) you want to zoom to

    - Note: Even though the objects are hidden, they can still be selected in the drawing

3. Run the Zoom To Selected extension from the Assistant

4. The extension will:
    - Temporarily show the hidden objects
    - Zoom to them
    - Hide them again automatically

## Features
- Automatically identifies hidden objects from the selection
- Temporarily shows only the hidden objects that are selected
- Returns the objects to hidden state after zooming
- Works with multiple selected objects

