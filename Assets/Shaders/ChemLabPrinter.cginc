#ifndef CHEMLAB_PRINTER_INCLUDED
#define CHEMLAB_PRINTER_INCLUDED
float _KPrinterActive;
float _KPrinterHeight;
void KPrinterClip(float3 worldPosition) {
    clip(lerp(1.0, _KPrinterHeight - worldPosition.y, _KPrinterActive));
}
#endif
