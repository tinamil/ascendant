#version 430

in vec2 colorCoord;
in vec3 vertexNormal;
in vec3 cameraSpacePosition;

out vec4 outputColor;

uniform sampler2D diffuseColorTex;

layout(std140) uniform;

uniform Material
{
	vec4 diffuseColor;
	vec4 specularColor;
	vec4 specularShininess;
} Mtl;

const int numberOfLights = 16;

struct PerLight
{
	vec4 cameraSpaceLightPos;
	vec4 lightIntensity;
};

uniform PerLightBlock
{
	PerLight PLight[numberOfLights];
};

uniform Light
{
	vec4 ambientIntensity;
	vec4 attenuationMaxGamma;
} Lgt;


float CalcAttenuation(in vec3 cameraSpacePosition, in vec3 cameraSpaceLightPos,	out vec3 lightDirection){
	vec3 lightDifference =  cameraSpaceLightPos - cameraSpacePosition;
	float lightDistanceSqr = dot(lightDifference, lightDifference);
	lightDirection = lightDifference * inversesqrt(lightDistanceSqr);
	
	return (1 / ( 1.0 + Lgt.attenuationMaxGamma.x * lightDistanceSqr));
}

vec4 ComputeLighting(in PerLight lightData)
{
	vec3 lightDir;
	vec4 lightIntensity;
	if(lightData.cameraSpaceLightPos.w == 0.0)
	{
		lightDir = vec3(lightData.cameraSpaceLightPos);
		lightIntensity = lightData.lightIntensity;
	}
	else
	{
		float atten = CalcAttenuation(cameraSpacePosition,
			lightData.cameraSpaceLightPos.xyz, lightDir);
		lightIntensity = atten * lightData.lightIntensity;
	}
	
	vec3 surfaceNormal = normalize(vertexNormal);
	float cosAngIncidence = dot(surfaceNormal, lightDir);
	cosAngIncidence = cosAngIncidence < 0.0001 ? 0.0 : cosAngIncidence;
	
	vec3 viewDirection = normalize(-cameraSpacePosition);
	
	vec3 halfAngle = normalize(lightDir + viewDirection);
	float angleNormalHalf = acos(dot(halfAngle, surfaceNormal));
	float exponent = angleNormalHalf / Mtl.specularShininess.x;
	exponent = -(exponent * exponent);
	float gaussianTerm = exp(exponent);

	gaussianTerm = cosAngIncidence != 0.0 ? gaussianTerm : 0.0;
	
	vec4 lighting = Mtl.diffuseColor * lightIntensity * cosAngIncidence;
	lighting += Mtl.specularColor * lightIntensity * gaussianTerm;
	
	return lighting;
}

void main()
{
	vec4 gamma = vec4(Lgt.attenuationMaxGamma.z);
	gamma.w = 1.0;
	vec4 diffuseColor = pow(texture(diffuseColorTex, colorCoord), 1/gamma) + Mtl.diffuseColor;
	vec4 accumLighting = diffuseColor * Lgt.ambientIntensity;
	for(int light = 0; light < numberOfLights; light++){
		accumLighting += ComputeLighting(PLight[light]);
	}
	accumLighting = accumLighting / Lgt.attenuationMaxGamma.y; 
	//outputColor = pow(accumLighting, gamma);
	outputColor = accumLighting;
}
