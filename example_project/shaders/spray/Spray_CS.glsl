#version 430

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in; 

// Structure that holds the draw command data for indirect rendering
struct DrawElementsIndirectCommand {
    uint count;       
    uint instanceCount;
    uint firstIndex;   
    uint baseVertex;     
    uint baseInstance;   
};

// Output buffer for vertices
layout(std430, binding = 0) 
buffer VertexBuffer {
    vec4 vertices[4];
};

// Output buffer for vertex-indices
layout(std430, binding = 1) 
buffer IndexBuffer {
    uint indices[];   // Array for indices
};

// Output buffer for the indirect draw command
layout(std430, binding = 2) 
buffer IndirectBuffer {
    DrawElementsIndirectCommand draw_arrays_indirect_command;
};

layout(std430, binding = 6) 
buffer UVBuffer {
    vec2 uvs[4];
};

layout(binding = 3, offset = 0) 
uniform atomic_uint visibleCount;

const vec2 mesh_uvs[4] = vec2[](
    vec2(0.0, 0.0), // bottom-left
    vec2(1.0, 0.0), // bottom-right
    vec2(1.0, 1.0), // top-right
    vec2(0.0, 1.0)  // top-left
);

const vec4 mesh[4] = vec4[](
    vec4(-0.5, -0.5, 0.0, 1.0), // bottom-left
    vec4( 0.5, -0.5, 0.0, 1.0), // bottom-right
    vec4( 0.5, 0.5, 0.0, 1.0), // top-right
    vec4(-0.5, 0.5, 0.0, 1.0)  // top-left
);

const uint mesh_indices[] = uint[](
    0, 1, 2,
    0, 2, 3
);

void setupIndirectRenderCommand(){
    uint count = mesh.length();

    for (int i = 0; i < count; ++i) {
        vertices[i] = mesh[i];
        uvs[i] = mesh_uvs[i];    
    }

    uint indices_count = mesh_indices.length();
    for (int i = 0; i < indices_count; ++i) {
        indices[i] = mesh_indices[i];
    }

    uint finalCount = atomicCounter(visibleCount);

    draw_arrays_indirect_command.count = indices_count;      
    draw_arrays_indirect_command.instanceCount = finalCount;   
    draw_arrays_indirect_command.firstIndex = 0;              
    draw_arrays_indirect_command.baseVertex = 0;               
    draw_arrays_indirect_command.baseInstance = 0;             
}

void main() {
    setupIndirectRenderCommand();
}
