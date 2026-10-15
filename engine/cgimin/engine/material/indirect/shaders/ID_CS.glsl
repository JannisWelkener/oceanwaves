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
    vec4 vertices[15];
};

layout(std430, binding = 6) 
buffer UVBuffer {
    vec2 uvs[15];
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

// Contains the mapped IDs to recreate the original ID from the clipped one
layout(std430, binding = 5)
buffer IntArrayBuffer {
    int mappingBuffer[]; 
};

layout(binding = 3, offset = 0) 
uniform atomic_uint visibleCount;

layout(binding = 4, offset = 0) 
uniform atomic_uint workGroup;

uniform vec2 chunkPos;

const vec2 mesh_uvs[] = vec2[](
    vec2(0.559120, 0.000000),
    vec2(0.411017, 0.135516),
    vec2(0.411017, 0.000000),
    vec2(0.559120, 0.135516),
    vec2(0.415960, 0.312901),
    vec2(0.556994, 0.312901),
    vec2(0.420683, 0.473963),
    vec2(0.560706, 0.473963),
    vec2(0.427131, 0.619379),
    vec2(0.560809, 0.619529),
    vec2(0.437464, 0.748817),
    vec2(0.549748, 0.913380),
    vec2(0.466078, 0.921912),
    vec2(0.512729, 0.995074),
    vec2(0.558808, 0.749833)
);

const vec4 mesh[15] = vec4[](
    vec4( 0.632706,         0, 0, 1.0), 
    vec4(-0.959065,  2.794460, 0, 1.0),  
    vec4(-0.959065,         0, 0, 1.0),  
    vec4( 0.632706,  2.794460, 0, 1.0),  
    vec4(-0.905960,  6.459348, 0, 1.0), 
    vec4( 0.609820,  6.459348, 0, 1.0),  
    vec4(-0.855223,  9.786999, 0, 1.0),  
    vec4( 0.649737,  9.786999, 0, 1.0),  
    vec4(-0.785911, 12.791382, 0, 1.0),  
    vec4( 0.650830, 12.794482, 0, 1.0),  
    vec4(-0.674829, 15.465643, 0, 1.0),  
    vec4( 0.531978, 18.865614, 0, 1.0),  
    vec4(-0.367288, 19.041887, 0, 1.0),  
    vec4( 0.134108, 20.553465, 0, 1.0), 
    vec4( 0.629317, 15.486628, 0, 1.0)   
);

const uint mesh_indices[39] = uint[](
    0, 1, 2,  // f 1/1/1 2/2/1 3/3/1 .
    3, 4, 1,  // f 4/4/1 5/5/1 2/2/1
    5, 6, 4,  // f 6/6/1 7/7/1 5/5/1
    7, 8, 6,  // f 8/8/1 9/9/1 7/7/1
    9, 10, 8, // f 10/10/1 11/11/1 9/9/1
    10, 11, 12, // f 11/11/1 12/12/1 13/13/1
    12, 11, 13, // f 13/13/1 12/12/1 14/14/1
    0, 3, 1, // f 1/1/1 4/4/1 2/2/1 . 
    3, 5, 4, // f 4/4/1 6/6/1 5/5/1
    5, 7, 6, // f 6/6/1 8/8/1 7/7/1
    7, 9, 8, // f 8/8/1 10/10/1 9/9/1
    9, 14, 10, // f 10/10/1 15/15/1 11/11/1
    10, 14, 11 // f 11/11/1 15/15/1 12/12/1
);

uniform vec3 cameraPosition;
uniform vec3 cameraDirection;
uniform mat4 view_projection_matrix;

uniform bool disableGPUCulling;

const uint MAX_WORK_GROUPS = 64*64-1;

bool isInsideFrustum(vec3 position, float threshold) {
    vec4 clipSpacePos = view_projection_matrix * vec4(position, 1.0);

    float expandedW = clipSpacePos.w + threshold;

    if (clipSpacePos.x < -expandedW || clipSpacePos.x > expandedW) return false;
    if (clipSpacePos.y < -expandedW || clipSpacePos.y > expandedW) return false;
    if (clipSpacePos.z < -expandedW || clipSpacePos.z > expandedW) return false;

    return true;
}

float rand(int seed) {
    seed = (seed << 13) ^ seed;
    return 1.0 - float((seed * (seed * seed * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0;
}

vec3 calculateInstancePosition(int instanceID){
    int rowLen = 64;
    float CHUNK_SIZE = 250;
	float space = CHUNK_SIZE / rowLen;

    float random = rand(instanceID);
	float random2 = rand(instanceID+1000);

    float jitterX = (space * random) - (space / 2.0f);
	float jitterY = (space * random2) - (space / 2.0f);

    return vec3((instanceID % rowLen * space + space/2.0f + jitterX) * 0.02,-20 * 0.02,(int(instanceID / rowLen) * space + space/2.0f + jitterY)*0.02);
}

void doFrustumCulling(){
    int instanceID = int(gl_WorkGroupID.x) * 64 + int(gl_WorkGroupID.y);

    vec3 instancePosition = calculateInstancePosition(instanceID);

    instancePosition.xz += chunkPos / 50;

    /*
        if instance is inside camera-frustum, add to be rendered
    */
    if(isInsideFrustum(instancePosition.xyz,0.5f) || disableGPUCulling){  
        /*
            atomicAdd for multiple WorkGroups, should be rendered
        */
        int current = int(atomicCounterIncrement(visibleCount));
        /*
            add mapping: actual gl_InstanceID -> full gl_InstanceID
            This is needed to restore the original ID i.e. position from the reduced ID inside the vertex shader
            Example:
            max: 10, visible: 2 [0,1] => [4,6] 
        */
        mappingBuffer[current] = instanceID;
    }  
}

void setupIndirectRenderCommand(){
    uint count = mesh.length();

    // Fill the vertex buffer
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

    doFrustumCulling();
    uint finishedGroups = atomicCounterIncrement(workGroup);

    memoryBarrier();
    barrier();

    /*
        After all workgroups are completed
    */
    if(finishedGroups == MAX_WORK_GROUPS){
        setupIndirectRenderCommand();
    }

}
