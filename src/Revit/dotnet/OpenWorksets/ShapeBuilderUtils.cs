namespace OpenWorksets;
public static class ShapeBuilderUtils
{
    private static double _radius = 1;
    private static double _halfRadius => _radius / 2;
    private static double _height = 2;
    public static BRepBuilder CylinderBuilder()
    {
        // Naming convention for faces and edges: we assume that x is to the left and pointing down, y is horizontal and pointing to the right, z is up
        BRepBuilder brepBuilder = new BRepBuilder(BRepType.Solid);

        // The surfaces of the four faces.
        Frame basis = new Frame(new XYZ(_halfRadius, -_radius, 0), XYZ.BasisY, -XYZ.BasisX, XYZ.BasisZ);
        CylindricalSurface cylSurf = CylindricalSurface.Create(basis, _halfRadius);
        Plane top = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, _height));  // normal points outside the cylinder
        Plane bottom = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero); // normal points inside the cylinder

        // Add the four faces
        BRepBuilderGeometryId frontCylFaceId = brepBuilder.AddFace(BRepBuilderSurfaceGeometry.Create(cylSurf, null), false);
        BRepBuilderGeometryId backCylFaceId = brepBuilder.AddFace(BRepBuilderSurfaceGeometry.Create(cylSurf, null), false);
        BRepBuilderGeometryId topFaceId = brepBuilder.AddFace(BRepBuilderSurfaceGeometry.Create(top, null), false);
        BRepBuilderGeometryId bottomFaceId = brepBuilder.AddFace(BRepBuilderSurfaceGeometry.Create(bottom, null), true);

        // Geometry for the four semi-circular edges and two vertical linear edges
        BRepBuilderEdgeGeometry frontEdgeBottom = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(0, -_radius, 0), new XYZ(_radius, -_radius, 0), new XYZ(_halfRadius, -_halfRadius, 0)));
        BRepBuilderEdgeGeometry backEdgeBottom = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(_radius, -_radius, 0), new XYZ(0, -_radius, 0), new XYZ(_halfRadius, -_radius - _halfRadius, 0)));

        BRepBuilderEdgeGeometry frontEdgeTop = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(0, -_radius, _height), new XYZ(_radius, -_radius, _height), new XYZ(_halfRadius, -_halfRadius, _height)));
        BRepBuilderEdgeGeometry backEdgeTop = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(0, -_radius, _height), new XYZ(_radius, -_radius, _height), new XYZ(_halfRadius, -_radius - _halfRadius, _height)));

        BRepBuilderEdgeGeometry linearEdgeFront = BRepBuilderEdgeGeometry.Create(new XYZ(_radius, -_radius, 0), new XYZ(_radius, -_radius, _height));
        BRepBuilderEdgeGeometry linearEdgeBack = BRepBuilderEdgeGeometry.Create(new XYZ(0, -_radius, 0), new XYZ(0, -_radius, _height));

        // Add the six edges
        BRepBuilderGeometryId frontEdgeBottomId = brepBuilder.AddEdge(frontEdgeBottom);
        BRepBuilderGeometryId frontEdgeTopId = brepBuilder.AddEdge(frontEdgeTop);
        BRepBuilderGeometryId linearEdgeFrontId = brepBuilder.AddEdge(linearEdgeFront);
        BRepBuilderGeometryId linearEdgeBackId = brepBuilder.AddEdge(linearEdgeBack);
        BRepBuilderGeometryId backEdgeBottomId = brepBuilder.AddEdge(backEdgeBottom);
        BRepBuilderGeometryId backEdgeTopId = brepBuilder.AddEdge(backEdgeTop);

        // Loops of the four faces
        BRepBuilderGeometryId loopId_Top = brepBuilder.AddLoop(topFaceId);
        BRepBuilderGeometryId loopId_Bottom = brepBuilder.AddLoop(bottomFaceId);
        BRepBuilderGeometryId loopId_Front = brepBuilder.AddLoop(frontCylFaceId);
        BRepBuilderGeometryId loopId_Back = brepBuilder.AddLoop(backCylFaceId);

        // Add coedges for the loop of the front face
        brepBuilder.AddCoEdge(loopId_Front, linearEdgeBackId, false);
        brepBuilder.AddCoEdge(loopId_Front, frontEdgeTopId, false);
        brepBuilder.AddCoEdge(loopId_Front, linearEdgeFrontId, true);
        brepBuilder.AddCoEdge(loopId_Front, frontEdgeBottomId, true);
        brepBuilder.FinishLoop(loopId_Front);
        brepBuilder.FinishFace(frontCylFaceId);

        // Add coedges for the loop of the back face
        brepBuilder.AddCoEdge(loopId_Back, linearEdgeBackId, true);
        brepBuilder.AddCoEdge(loopId_Back, backEdgeBottomId, true);
        brepBuilder.AddCoEdge(loopId_Back, linearEdgeFrontId, false);
        brepBuilder.AddCoEdge(loopId_Back, backEdgeTopId, true);
        brepBuilder.FinishLoop(loopId_Back);
        brepBuilder.FinishFace(backCylFaceId);

        // Add coedges for the loop of the top face
        brepBuilder.AddCoEdge(loopId_Top, backEdgeTopId, false);
        brepBuilder.AddCoEdge(loopId_Top, frontEdgeTopId, true);
        brepBuilder.FinishLoop(loopId_Top);
        brepBuilder.FinishFace(topFaceId);

        // Add coedges for the loop of the bottom face
        brepBuilder.AddCoEdge(loopId_Bottom, frontEdgeBottomId, false);
        brepBuilder.AddCoEdge(loopId_Bottom, backEdgeBottomId, false);
        brepBuilder.FinishLoop(loopId_Bottom);
        brepBuilder.FinishFace(bottomFaceId);

        brepBuilder.Finish();

        return brepBuilder;
    }
}